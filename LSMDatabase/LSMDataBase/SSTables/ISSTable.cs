using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.SSTables
{
/// <summary>
/// 文件信息表 （存储在IO中）
/// 元数据 | 索引列表 | 数据区(数据修改只会新增，并修改索引列表数据) 
/// </summary>
public interface ISSTable : IDisposable
{
    /// <summary>
    /// 数据地址
    /// </summary>
    public string TableFilePath();
    /// <summary>
    /// 重写文件
    /// </summary>
    public void Write(List<KeyValue> values, int Level = 0);
    /// <summary>
    /// 数据位置
    /// </summary>
    public Dictionary<string, DataPosition> DataPositions { get; }
    /// <summary>
    /// 获取总数
    /// </summary>
    /// <returns></returns>
    public int Count { get; }
    /// <summary>
    /// 元数据
    /// </summary>
    public ITableMetaInfo FileTableMetaInfo { get; }
    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public KeyValue Search(string key);
    /// <summary>
    /// 有序的key列表
    /// </summary>
    /// <returns></returns>
    public List<string> SortIndexs();
    /// <summary>
    /// 获取位置
    /// </summary>
    DataPosition GetDataPosition(string key);
    /// <summary>
    /// 读取某个位置的值
    /// </summary>
    public byte[] ReadValue(DataPosition position);
    /// <summary>
    /// 加载所有数据
    /// </summary>
    /// <returns></returns>
    public List<KeyValue> ReadAll(bool incloudDeleted = true);
    /// <summary>
    /// 获取所有keys
    /// </summary>
    /// <returns></returns>
    public List<string> GetKeys();
    /// <summary>
    /// 获取表名
    /// </summary>
    /// <returns></returns>
    public long FileTableName();
    /// <summary>
    /// 文件的大小
    /// </summary>
    /// <returns></returns>
    public long FileBytes { get; }
    /// <summary>
    /// 获取级别
    /// </summary>
    public int GetLevel();
}
}
