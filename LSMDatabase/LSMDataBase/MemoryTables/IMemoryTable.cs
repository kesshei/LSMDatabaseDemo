using LSMDataBase.DataBases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.MemoryTables
{
    /// <summary>
    /// 内存表(排序树，二叉树)
    /// </summary>
    public interface IMemoryTable : IDisposable
    {
        IDataBaseConfig DataBaseConfig { get; }
        /// <summary>
        /// 获取总数
        /// </summary>
        int GetCount();
        /// <summary>
        /// 搜索(从新到旧，从大到小)
        /// </summary>
        KeyValue Search(string key);
        /// <summary>
        /// 设置新值
        /// </summary>
        void Set(KeyValue keyValue);
        /// <summary>
        /// 删除key
        /// </summary>
        void Delete(KeyValue keyValue);
        /// <summary>
        /// 获取所有 key 数据列表
        /// </summary>
        /// <returns></returns>
        IList<string> GetKeys();
        /// <summary>
        /// 获取所有数据
        /// </summary>
        /// <returns></returns>
        (List<KeyValue> keyValues, List<long> times) GetKeyValues(bool Immutable);
        /// <summary>
        /// 获取不变表的数量
        /// </summary>
        /// <returns></returns>
        int GetImmutableTableCount();
        /// <summary>
        /// 开始交换
        /// </summary>
        void Swap(List<long> times);
        /// <summary>
        /// 清空全部数据
        /// </summary>
        void Clear();
    }
}
