using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.MemoryTables
{
public class MemoryTableValue : IDisposable
{
    public long Time { get; set; } = IDHelper.MarkID();
    /// <summary>
    /// 是否是不可变
    /// </summary>
    public bool Immutable { get; set; } = false;
    /// <summary>
    /// 数据
    /// </summary>
    public Dictionary<string, KeyValue> Dic { get; set; } = new();

    public void Dispose()
    {
        Dic.Clear();
    }

    public override string ToString()
    {
        return $"Time {Time} Immutable：{Immutable}";
    }
}
}
