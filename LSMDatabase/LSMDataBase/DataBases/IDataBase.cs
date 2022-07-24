using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.DataBases
{
/// <summary>
/// 数据库接口
/// </summary>
public interface IDataBase : IDisposable
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    IDataBaseConfig DataBaseConfig { get; }
    /// <summary>
    /// 获取数据
    /// </summary>
    KeyValue Get(string key);
    /// <summary>
    /// 保存数据(或者更新数据)
    /// </summary>
    bool Set(KeyValue keyValue);
    /// <summary>
    /// 保存数据(或者更新数据)
    /// </summary>
    bool Set(string key, object value);
    /// <summary>
    /// 获取全部key
    /// </summary>
    List<string> GetKeys();
    /// <summary>
    /// 删除指定数据，并返回存在的数据
    /// </summary>
    KeyValue DeleteAndGet(string key);
    /// <summary>
    /// 删除数据
    /// </summary>
    void Delete(string key);
    /// <summary>
    /// 定时检查
    /// </summary>
    void Check(object state);
    /// <summary>
    /// 清除数据库所有数据
    /// </summary>
    void Clear();
}
}
