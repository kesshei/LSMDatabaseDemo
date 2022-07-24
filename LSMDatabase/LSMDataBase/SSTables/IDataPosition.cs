using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.SSTables
{
    /// <summary>
    /// 数据的位置
    /// </summary>
    public interface IDataPosition
    {
        /// <summary>
        /// 索引起始位置
        /// </summary>
        public long IndexStart { get; set; }
        /// <summary>
        /// 开始地址
        /// </summary>
        public long Start { get; set; }
        /// <summary>
        /// 数据长度
        /// </summary>
        public long Length { get; set; }
        /// <summary>
        /// key的长度
        /// </summary>
        public long KeyLength { get; set; }
        /// <summary>
        /// 是否已经删除
        /// </summary>
        public bool Deleted { get; set; }
        public byte[] GetBytes();
    }
}
