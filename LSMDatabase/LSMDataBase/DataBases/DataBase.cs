using LSMDataBase.DataBases;
using LSMDataBase.MemoryTables;
using LSMDataBase.WalLogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase
{
    /// <summary>
    /// LSM数据库实现
    /// </summary>
    public class DataBase : IDataBase
    {
        public IDataBaseConfig DataBaseConfig { get; private set; }
        private IWalLog WalLog { get; set; }
        private IMemoryTable MemoryTable { get; set; }
        private ITableManage TableManage { get; set; }
        private ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim();
        private Timer Timer;
        public DataBase(IDataBaseConfig config)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Log.Info($"启动数据服务!");
            if (!Directory.Exists(config.DataDir))
            {
                Directory.CreateDirectory(config.DataDir);
            }
            Log.Info($"数据库文件目录:{config.DataDir}");

            DataBaseConfig = config;
            WalLog = new WalLog(config);
            TableManage = new TableManage(config);
            MemoryTable = WalLog.LoadToMemory();
            Timer = new Timer(new TimerCallback(Check), null, 0, config.CheckInterval);
            stopwatch.Stop();
            Log.Info($"数据库服务启动成功，耗时:{stopwatch.ElapsedMilliseconds} 毫秒!");
        }
        public KeyValue Get(string key)
        {
            ReaderWriterLock.EnterReadLock();
            try
            {
                var result = MemoryTable.Search(key);
                if (result.IsExist())
                {
                    return result;
                }
                return TableManage.Search(key);
            }
            finally
            {
                ReaderWriterLock.ExitReadLock();
            }
        }
        public List<string> GetKeys()
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (var item in MemoryTable.GetKeys())
            {
                keys.Add(item);
            }
            foreach (var item in TableManage.GetKeys())
            {
                keys.Add(item);
            }
            return keys.ToList();
        }
        public bool Set(KeyValue keyValue)
        {
            ReaderWriterLock.EnterWriteLock();
            try
            {
                WalLog.Write(keyValue);
                MemoryTable.Set(keyValue);
                return true;
            }
            finally
            {
                ReaderWriterLock.ExitWriteLock();
            }
        }
        public bool Set(string key, object value)
        {
            return Set(new KeyValue(key, value));
        }
        public void Delete(string key)
        {
            ReaderWriterLock.EnterWriteLock();
            try
            {
                var deleteKV = new KeyValue(key, null, true);
                WalLog.Write(deleteKV);
                MemoryTable.Delete(deleteKV);
            }
            finally
            {
                ReaderWriterLock.ExitWriteLock();
            }
        }
        public KeyValue DeleteAndGet(string key)
        {
            var oldValue = Get(key);
            Delete(key);
            return oldValue;
        }
        private volatile bool IsProcess = false;
        public void Check(object state)
        {
            //Log.Info($"定时心跳检查!");
            if (IsProcess)
            {
                return;
            }
            if (ClearState)
            {
                return;
            }
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                IsProcess = true;
                checkMemory();
                TableManage.Check();
                stopwatch.Stop();
                GC.Collect();
                Log.Info($"定时心跳处理耗时:{stopwatch.ElapsedMilliseconds}毫秒");
            }
            finally
            {
                IsProcess = false;
            }
        }
        /// <summary>
        /// 检查内存
        /// </summary>
        private void checkMemory()
        {
            ReaderWriterLock.EnterUpgradeableReadLock();
            try
            {
                if (MemoryTable.GetImmutableTableCount() > 0)
                {
                    ReaderWriterLock.EnterWriteLock();
                    try
                    {
                        //获取不变表的数据
                        (List<KeyValue> keyValues, List<long> times) = MemoryTable.GetKeyValues(true);
                        if (keyValues?.Any() == true)
                        {
                            Stopwatch stopwatch = Stopwatch.StartNew();
                            var tempData = MemoryTable.GetKeyValues(false);
                            Log.Info($"内存表开始落地：{keyValues.Count}条数据");
                            TableManage.CreateNewTable(keyValues);
                            WalLog.Reset();
                            WalLog.Write(tempData.keyValues);
                            MemoryTable.Swap(times);
                            stopwatch.Stop();
                            Log.Info($"内存表落地结束耗时{stopwatch.ElapsedMilliseconds}毫秒");

                            keyValues.Clear();
                            keyValues = null;
                        }
                    }
                    finally
                    {
                        ReaderWriterLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                ReaderWriterLock.ExitUpgradeableReadLock();
            }
        }
        private bool ClearState = false;
        public void Clear()
        {
            ReaderWriterLock.EnterWriteLock();
            try
            {
                ClearState = true;
                SpinWait.SpinUntil(() => !IsProcess, 10 * 1000);
                WalLog.Reset();
                MemoryTable.Clear();
                TableManage.Clear();
            }
            finally
            {
                ClearState = false;
                ReaderWriterLock.ExitWriteLock();
            }
        }
        public void Dispose()
        {
            Timer.Dispose();
            WalLog?.Dispose();
            MemoryTable?.Dispose();
            TableManage?.Dispose();
        }
    }
}
