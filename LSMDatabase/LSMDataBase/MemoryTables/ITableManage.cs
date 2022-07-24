using LSMDataBase.DataBases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.MemoryTables
{
/// <summary>
/// 表管理项
/// </summary>
public interface ITableManage : IDisposable
{
    IDataBaseConfig DataBaseConfig { get; }
    /// <summary>
    /// 搜索(从新到老,从大到小)
    /// </summary>
    KeyValue Search(string key);
    /// <summary>
    /// 获取全部key
    /// </summary>
    List<string> GetKeys();
    /// <summary>
    /// 检查数据库文件，如果文件无效数据太多，就会触发整合文件
    /// </summary>
    void Check();
    /// <summary>
    /// 创建一个新Table
    /// </summary>
    void CreateNewTable(List<KeyValue> values, int Level = 0);
    /// <summary>
    /// 清理某个级别的数据
    /// </summary>
    /// <param name="Level"></param>
    public void Remove(int Level);
    /// <summary>
    /// 清除数据
    /// </summary>
    public void Clear();
}
}
