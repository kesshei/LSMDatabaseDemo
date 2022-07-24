using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.SSTables
{
    public class DataPosition : IDataPosition
    {
        public DataPosition() { }
        public DataPosition(long Start, long Length, bool Deleted = false)
        {
            this.Start = Start;
            this.Length = Length;
            this.Deleted = Deleted;
        }
        public long IndexStart { get; set; }
        public long Start { get; set; }
        public long Length { get; set; }
        public long KeyLength { get; set; }
        public bool Deleted { get; set; }
        public static int GetDataLength()
        {
            return sizeof(long) * 4 + sizeof(bool);
        }
        public byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(IndexStart));
            bytes.AddRange(BitConverter.GetBytes(Start));
            bytes.AddRange(BitConverter.GetBytes(Length));
            bytes.AddRange(BitConverter.GetBytes(KeyLength));
            bytes.AddRange(BitConverter.GetBytes(Deleted));
            return bytes.ToArray();
        }

        public static DataPosition GetDataPosition(byte[] bytes)
        {
            DataPosition dataPosition = new DataPosition();
            var longSize = sizeof(long);
            var index = 0;
            dataPosition.IndexStart = BitConverter.ToInt64(bytes, index += index);
            dataPosition.Start = BitConverter.ToInt64(bytes, index += longSize);
            dataPosition.Length = BitConverter.ToInt64(bytes, index += longSize);
            dataPosition.KeyLength = BitConverter.ToInt64(bytes, index += longSize);
            dataPosition.Deleted = BitConverter.ToBoolean(bytes, index += longSize);
            return dataPosition;
        }
    }
}
