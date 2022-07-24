using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.DataBases
{
/// <summary>
/// 数据库相关配置
/// </summary>
public interface IDataBaseConfig
{
    /// <summary>
    /// 数据库数据目录
    /// </summary>
    public string DataDir { get; set; }
    /// <summary>
    /// 0 层的 所有 SsTable 文件大小总和的最大值，单位 MB，超过此值，该层 SsTable 将会被压缩到下一层
    /// 每层数据大小是上层的N倍
    /// </summary>
    public int Level0Size { get; set; }
    /// <summary>
    /// 层与层之间的倍数
    /// </summary>
    public int LevelMultiple { get; set; }
    /// <summary>
    /// 每层数量阈值
    /// </summary>
    public int LevelCount { get; set; }
    /// <summary>
    /// 内存表的 kv 最大数量，超出这个阈值，内存表将会被保存到 SsTable 中
    /// </summary>
    public int MemoryTableCount { get; set; }
    /// <summary>
    /// 压缩内存、文件的时间间隔，多久进行一次检查工作
    /// </summary>
    public int CheckInterval { get; set; }
}
}
