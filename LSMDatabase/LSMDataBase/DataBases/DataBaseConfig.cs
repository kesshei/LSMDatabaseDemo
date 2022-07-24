using LSMDataBase.DataBases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    public class DataBaseConfig : IDataBaseConfig
    {
        public string DataDir { get; set; }
        public int Level0Size { get; set; } = 10;
        public int LevelMultiple { get; set; } = 10;
        public int LevelCount { get; set; } = 8000;
        public int MemoryTableCount { get; set; } = 10000;
        public int CheckInterval { get; set; } = 1000;
        public static long Version { get; set; } = 202207202304;
    }
}
