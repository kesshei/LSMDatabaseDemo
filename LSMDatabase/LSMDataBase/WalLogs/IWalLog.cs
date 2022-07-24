using LSMDataBase.DataBases;
using LSMDataBase.MemoryTables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.WalLogs
{
    /// <summary>
    /// 日志
    /// </summary>
    public interface IWalLog : IDisposable
    {
        /// <summary>
        /// 数据库配置
        /// </summary>
        IDataBaseConfig DataBaseConfig { get; }
        /// <summary>
        /// 加载Wal日志到内存表
        /// </summary>
        /// <returns></returns>
        IMemoryTable LoadToMemory();
        /// <summary>
        /// 写日志
        /// </summary>
        void Write(KeyValue data);
        /// <summary>
        /// 写日志
        /// </summary>
        void Write(List<KeyValue> data);
        /// <summary>
        /// 重置日志文件
        /// </summary>
        void Reset();
    }
}
