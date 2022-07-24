using LSMDataBase.DataBases;
using LSMDataBase.MemoryTables;
using System.Buffers;

namespace LSMDataBase.WalLogs
{
    /// <summary>
    /// 日志管理
    /// 长度:内容
    /// </summary>
    public class WalLog : IWalLog
    {
        private string LogName = "wal.log";
        public IDataBaseConfig DataBaseConfig { get; private set; }
        private IMemoryTable MemoryTable;
        private string LogPath;
        private ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim();
        private FileStream LogFile;
        public WalLog(IDataBaseConfig config)
        {
            DataBaseConfig = config;
            MemoryTable = new MemoryTable(config);
            LogPath = Path.Combine(DataBaseConfig.DataDir, LogName);
            LogFile = File.Open(LogPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }
        public IMemoryTable LoadToMemory()
        {
            if (File.Exists(LogPath))
            {
                MemoryTable = Load();
            }
            return MemoryTable;
        }
        /// <summary>
        /// 数据格式 json key:value
        /// </summary>
        /// <returns></returns>
        private IMemoryTable Load()
        {
            ReaderWriterLock.EnterReadLock();
            var shard = ArrayPool<byte>.Shared;
            var RentLength = 4;
            byte[] bytes = shard.Rent(RentLength);//int 长度
            try
            {
                IMemoryTable memoryTable = new MemoryTable(DataBaseConfig);
                int numBytesToRead = (int)LogFile.Length;
                LogFile.Seek(0, SeekOrigin.Begin);
                while (numBytesToRead > 0)
                {
                    var n = LogFile.Read(bytes, 0, RentLength);
                    if (n == 0)
                    {
                        break;
                    }
                    numBytesToRead -= n;

                    var RentDataLength = BitConverter.ToInt32(bytes.Take(RentLength).ToArray());

                    var dataBytes = shard.Rent(RentDataLength);
                    n = LogFile.Read(dataBytes, 0, RentDataLength);
                    if (n == 0)
                    {
                        break;
                    }
                    var value = dataBytes.Take(RentDataLength).ToArray().AsObject<KeyValue>();
                    if (value != null)
                    {
                        memoryTable.Set(value);
                    }
                    shard.Return(dataBytes);

                    numBytesToRead -= n;
                }
                return memoryTable;
            }
            finally
            {
                shard.Return(bytes);
                ReaderWriterLock.ExitReadLock();
            }
        }
        public void Reset()
        {
            ReaderWriterLock.EnterWriteLock();
            try
            {
                LogFile.SetLength(0);
            }
            finally
            {
                ReaderWriterLock.ExitWriteLock();
            }
        }

        public void Write(KeyValue data)
        {
            ReaderWriterLock.EnterWriteLock();
            try
            {
                LogFile.Seek(0, SeekOrigin.End);
                var bytes = data.AsBytes();
                var lengthBytes = BitConverter.GetBytes(bytes.Length);
                LogFile.Write(lengthBytes, 0, lengthBytes.Length);
                LogFile.Write(bytes, 0, bytes.Length);
                LogFile.Flush();
                bytes = null;
            }
            finally
            {
                ReaderWriterLock.ExitWriteLock();
            }
        }
        public void Write(List<KeyValue> datas)
        {
            ReaderWriterLock.EnterWriteLock();
            try
            {
                LogFile.Seek(0, SeekOrigin.End);
                foreach (var data in datas)
                {
                    var bytes = data.AsBytes();
                    var lengthBytes = BitConverter.GetBytes(bytes.Length);
                    LogFile.Write(lengthBytes, 0, lengthBytes.Length);
                    LogFile.Write(bytes, 0, bytes.Length);
                    bytes = null;
                }
                LogFile.Flush();
            }
            finally
            {
                ReaderWriterLock.ExitWriteLock();
            }
        }
        public void Dispose()
        {
            LogFile?.Dispose();
        }
    }
}
