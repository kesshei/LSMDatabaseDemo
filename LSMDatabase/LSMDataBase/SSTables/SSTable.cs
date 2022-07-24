using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.SSTables
{
    public class SSTable : ISSTable
    {
        private string dbFile;
        private bool IsDir;
        public SSTable(string dbFile, bool IsDir = true)
        {
            this.dbFile = dbFile;
            this.IsDir = IsDir;
        }

        public string TableFilePath()
        {
            if (!IsDir)
            {
                return dbFile;
            }
            if (FileTableMetaInfo == null)
            {
                return null;
            }
            return Path.Combine(dbFile, $"{GetLevel()}_{FileTableMetaInfo.Time}.db");
        }

        public ITableMetaInfo FileTableMetaInfo { get; private set; }

        public Dictionary<string, DataPosition> DataPositions { get; private set; }

        public long FileBytes
        {
            get
            {
                return new FileInfo(TableFilePath()).Length;
            }
        }

        public int Count => DataPositions.Count;

        public int GetLevel()
        {
            return FileTableMetaInfo.Level;
        }

        public long FileTableName()
        {
            return FileTableMetaInfo.Time;
        }

        public List<string> SortIndexs()
        {
            var list = DataPositions.Keys.ToList();
            list.Sort();
            return list;
        }
        public void Load()
        {
            var path = TableFilePath();
            FileTableMetaInfo = new TableMetaInfo();
            var size = TableMetaInfo.GetDataLength();
            using var read = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var shard = ArrayPool<byte>.Shared;
            var RentLength = size;
            var MetaInfoBytes = shard.Rent(RentLength);
            read.Seek(read.Length - size, SeekOrigin.Begin);
            read.Read(MetaInfoBytes, 0, RentLength);

            FileTableMetaInfo = TableMetaInfo.GetFileMetaInfo(MetaInfoBytes.Take(RentLength).ToArray());
            shard.Return(MetaInfoBytes);
            //读取索引部分
            DataPositions = new Dictionary<string, DataPosition>();
            RentLength = DataPosition.GetDataLength();
            read.Seek(FileTableMetaInfo.IndexStart, SeekOrigin.Begin);
            while (read.Position < FileTableMetaInfo.IndexStart + FileTableMetaInfo.IndexLength)
            {
                var DataPositionBytes = shard.Rent(RentLength);
                read.Read(DataPositionBytes, 0, RentLength);
                var dataPosition = DataPosition.GetDataPosition(DataPositionBytes.Take(RentLength).ToArray());
                shard.Return(DataPositionBytes);

                var keyRentLength = (int)dataPosition.KeyLength;
                var keyBytes = shard.Rent(keyRentLength);
                read.Read(keyBytes, 0, keyRentLength);
                var key = Encoding.UTF8.GetString(keyBytes.Take(keyRentLength).ToArray());
                shard.Return(keyBytes);
                DataPositions.Add(key, dataPosition);
            }
        }
        /// <summary>
        /// 重写文件(会采用冗余的方式，实现重写文件)
        /// </summary>
        public void Write(List<KeyValue> values, int Level = 0)
        {
            if (values.Any() == false)
            {
                return;
            }
            var tableId = IDHelper.MarkID();
            DataPositions = new Dictionary<string, DataPosition>();
            var ValueBytes = new List<byte>();

            foreach (var item in values)
            {
                var value = item.DataValue;
                DataPositions.Add(item.Key, new DataPosition(ValueBytes.Count(), value.Length, item.Deleted));
                ValueBytes.AddRange(value);
            }
            //赋予index
            var index = 1;
            var DataPositionBytes = new List<byte>();
            var indexRoot = ValueBytes.Count();
            foreach (var item in DataPositions)
            {
                var keyBytes = Encoding.UTF8.GetBytes(item.Key);
                item.Value.KeyLength = keyBytes.Length;
                item.Value.IndexStart = indexRoot + DataPositionBytes.Count();

                DataPositionBytes.AddRange(item.Value.GetBytes());
                DataPositionBytes.AddRange(keyBytes);
                index++;
            }
            FileTableMetaInfo = new TableMetaInfo(tableId, 0, ValueBytes.Count(), ValueBytes.Count(), DataPositionBytes.Count, Level);
            var MetaInfoBytes = FileTableMetaInfo.GetBytes();

            var path = TableFilePath();
            using var writer = File.OpenWrite(path);
            writer.Write(ValueBytes.ToArray());
            writer.Write(DataPositionBytes.ToArray());
            writer.Write(MetaInfoBytes.ToArray());

            //clear
            ValueBytes.Clear();
            ValueBytes = null;
            DataPositionBytes.Clear();
            DataPositionBytes = null;
            MetaInfoBytes.Clear();
            MetaInfoBytes = null;
        }
        public KeyValue Search(string key)
        {
            if (DataPositions == null)
            {
                return null;
            }
            var keyvalue = KeyValue.Null;
            keyvalue.Key = key;
            if (DataPositions.TryGetValue(key, out var position))
            {
                var result = ReadValue(position);
                keyvalue.DataValue = result;
                keyvalue.Deleted = position.Deleted;
            }
            return keyvalue;
        }
        public List<string> GetKeys()
        {
            return DataPositions.Select(t => t.Key).ToList();
        }
        public DataPosition GetDataPosition(string key)
        {
            DataPositions.TryGetValue(key, out var position);
            return position;
        }

        public byte[] ReadValue(DataPosition position)
        {

            if (position == null || position.Deleted || position.Length == 0)
            {
                return null;
            }
            var path = TableFilePath();
            using var read = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            read.Seek(position.Start, SeekOrigin.Begin);

            var values = new byte[position.Length];
            read.Read(values, 0, values.Length);

            return values;
        }
        public List<KeyValue> ReadAll(bool incloudDeleted = true)
        {
            List<KeyValue> keyValues = new List<KeyValue>();
            var path = TableFilePath();
            using var read = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var positions = new List<KeyValuePair<string, DataPosition>>();
            if (incloudDeleted)
            {
                positions = DataPositions.ToList();
            }
            else
            {
                positions = DataPositions.Where(t => !t.Value.Deleted).ToList();
            }

            foreach (var keyValue in positions)
            {
                read.Seek(keyValue.Value.Start, SeekOrigin.Begin);
                var valuebytes = new byte[keyValue.Value.Length];
                read.Read(valuebytes, 0, valuebytes.Length);
                keyValues.Add(new KeyValue(keyValue.Key, valuebytes, keyValue.Value.Deleted));
            }
            return keyValues;
        }
        public static SSTable CreateFileTable(string dbFile, List<KeyValue> values, int Level = 0)
        {
            if (values?.Any() == false)
            {
                return null;
            }
            var SSTable = new SSTable(dbFile);
            SSTable.Write(values, Level);
            if (SSTable.FileTableMetaInfo != null)
            {
                return SSTable;
            }
            return null;
        }
        public void Dispose()
        {
            DataPositions.Clear();
        }
        public override string ToString()
        {
            return $"{FileTableName()} Level:{GetLevel()}";
        }
    }
}
