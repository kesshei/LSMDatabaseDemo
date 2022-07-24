using LSMDataBase.DataBases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.SSTables
{
    public class TableMetaInfo : ITableMetaInfo
    {
        public TableMetaInfo() { }
        public TableMetaInfo(long Time, long DataStart, long DataLength, long IndexStart, long IndexLength, int Level = 0)
        {
            Version = DataBaseConfig.Version;
            this.Time = Time;
            this.DataStart = DataStart;
            this.DataLength = DataLength;
            this.IndexStart = IndexStart;
            this.IndexLength = IndexLength;
            this.Level = Level;
        }
        public long Version { get; set; }
        public long Time { get; set; }
        public long DataStart { get; set; }
        public long DataLength { get; set; }
        public long IndexStart { get; set; }
        public long IndexLength { get; set; }
        public int Level { get; set; }

        public static int GetDataLength()
        {
            return sizeof(long) * 6 + sizeof(int);
        }
        public List<byte> GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(Time));
            bytes.AddRange(BitConverter.GetBytes(DataStart));
            bytes.AddRange(BitConverter.GetBytes(DataLength));
            bytes.AddRange(BitConverter.GetBytes(IndexStart));
            bytes.AddRange(BitConverter.GetBytes(IndexLength));
            bytes.AddRange(BitConverter.GetBytes(Level));
            return bytes;
        }
        public static TableMetaInfo GetFileMetaInfo(byte[] bytes)
        {
            TableMetaInfo fileMetaInfo = new TableMetaInfo();
            var longSize = sizeof(long);
            var index = 0;
            fileMetaInfo.Version = BitConverter.ToInt64(bytes, index);
            fileMetaInfo.Time = BitConverter.ToInt64(bytes, index += longSize);
            fileMetaInfo.DataStart = BitConverter.ToInt64(bytes, index += longSize);
            fileMetaInfo.DataLength = BitConverter.ToInt64(bytes, index += longSize);
            fileMetaInfo.IndexStart = BitConverter.ToInt64(bytes, index += longSize);
            fileMetaInfo.IndexLength = BitConverter.ToInt64(bytes, index += longSize);
            fileMetaInfo.Level = BitConverter.ToInt32(bytes, index += longSize);
            return fileMetaInfo;
        }
    }
}
