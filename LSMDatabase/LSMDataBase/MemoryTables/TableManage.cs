using LSMDataBase.DataBases;
using LSMDataBase.SSTables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.MemoryTables
{
    /// <summary>
    /// 每个库都有多个表，表包含多个 索引表和数据表
    /// </summary>
    public class TableManage : ITableManage
    {
        public IDataBaseConfig DataBaseConfig { get; private set; }
        /// <summary>
        /// 从小到大排序的
        /// </summary>
        public SortedList<long, ISSTable> fileTables;
        private ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim();
        public TableManage(IDataBaseConfig config)
        {
            DataBaseConfig = config;
            fileTables = LoadDataBaseFile();
        }
        public SortedList<long, ISSTable> LoadDataBaseFile()
        {
            var list = new SortedList<long, ISSTable>();
            foreach (var item in Directory.GetFiles(DataBaseConfig.DataDir, "*.db"))
            {
                var SSTable = new SSTable(item, false);
                SSTable.Load();
                list.Add(SSTable.FileTableName(), SSTable);
            }
            return list;
        }
        /// <summary>
        /// 检查数据库文件，如果文件无效数据太多，就会触发整合文件
        /// </summary>
        public void Check()
        {
            foreach (var item in fileTables.GroupBy(t => t.Value.GetLevel()))
            {
                var level = item.Key;
                var AllSize = item.Sum(t => t.Value.FileBytes);
                var AllCount = item.Sum(t => t.Value.Count);
                var TableSize = AllSize / 1024 / 1024;//转 MB
                var CurrentLevelMaxSize = Math.Pow(DataBaseConfig.LevelMultiple, level) * DataBaseConfig.Level0Size;
                var CurrentLevelCount = Math.Pow(DataBaseConfig.LevelMultiple, level) * DataBaseConfig.LevelCount;
                if (TableSize > CurrentLevelMaxSize || AllCount > CurrentLevelCount)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    Log.Info($"持久化表级别Level {level} 当前数据 Size:{TableSize} Count:{AllCount} 触及阈值 Level:{level} MaxSize:{CurrentLevelMaxSize} MaxCount:{CurrentLevelCount}， 开始压缩!");
                    SegmentCompaction(level, item.Select(t => t.Value).ToList());
                    stopwatch.Stop();
                    Log.Info($"持久化表级别:{level} 压缩完毕，耗时:{stopwatch.ElapsedMilliseconds} 毫秒!");
                }
            }
        }
        /// <summary>
        /// 分级压缩文件(先老后新，先小后大,级别 从0 到N )
        /// 数据满了一定阈值，就会生成一个sstable文件
        /// 压缩后，级别自动往下一级
        /// </summary>
        private void SegmentCompaction(int level, List<ISSTable> levels)
        {
            var nextLevel = level + 1;
            var dic = new Dictionary<string, KeyValue>();
            //倒着遍历 先老后新，先小后大
            foreach (var item in levels.OrderBy(t => t.FileTableName()))
            {
                var allData = item.ReadAll(false);
                foreach (var data in allData)
                {
                    dic[data.Key] = new KeyValue(data.Key, data.DataValue, data.Deleted);
                }
                allData.Clear();
                allData = null;
            }
            if (dic.Values?.Count() > 0)
            {
                CreateNewTable(dic.Values.ToList(), nextLevel);
                Remove(level);

                dic.Clear();
                dic = null;
            }
        }
        /// <summary>
        /// 搜索(从新到老,从大到小)
        /// </summary>
        public KeyValue Search(string key)
        {
            foreach (var item in fileTables.GroupBy(t => t.Value.GetLevel()).OrderBy(t => t.Key))
            {
                foreach (var table in item.OrderByDescending(t => t.Key))
                {
                    var result = table.Value.Search(key);
                    if (result?.IsExist() == true)
                    {
                        return result;
                    }
                }
            }
            return KeyValue.Null;
        }
        public List<string> GetKeys()
        {
            ReaderWriterLock.EnterReadLock();
            try
            {
                return fileTables.Values.SelectMany(t => t.GetKeys()).Distinct().ToList();
            }
            finally
            {
                ReaderWriterLock.ExitReadLock();
            }
        }
        /// <summary>
        /// 清理某个级别的数据
        /// </summary>
        /// <param name="Level"></param>
        public void Remove(int level)
        {
            ReaderWriterLock.EnterWriteLock();
            try
            {
                var filePaths = fileTables.Where(t => t.Value.GetLevel() == level).Select(t => t.Value.TableFilePath()).ToList();
                var removeIds = fileTables.Where(t => t.Value.GetLevel() == level).Select(t => t.Key).ToList();
                foreach (var item in removeIds)
                {
                    fileTables[item].Dispose();
                    fileTables.Remove(item);
                }
                foreach (var file in filePaths)
                {
                    File.Delete(file);
                }
            }
            finally
            {
                ReaderWriterLock.ExitWriteLock();
            }
        }

        public void CreateNewTable(List<KeyValue> values, int Level = 0)
        {
            ReaderWriterLock.EnterWriteLock();
            try
            {
                var table = SSTable.CreateFileTable(DataBaseConfig.DataDir, values, Level);
                if (table != null)
                {
                    fileTables.Add(table.FileTableName(), table);
                }
            }
            finally
            {
                ReaderWriterLock.ExitWriteLock();
            }
        }
        public void Dispose()
        {
            foreach (var item in fileTables)
            {
                item.Value.Dispose();
            }
        }

        public void Clear()
        {
            ReaderWriterLock.EnterWriteLock();
            try
            {
                var result = fileTables.ToList();
                fileTables.Clear();
                foreach (var item in result)
                {
                    File.Delete(item.Value.TableFilePath());
                    item.Value.Dispose();
                }
            }
            finally
            {
                ReaderWriterLock.ExitWriteLock();
            }
        }
    }
}
