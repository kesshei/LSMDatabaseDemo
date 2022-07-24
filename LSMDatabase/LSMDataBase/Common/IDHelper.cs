using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase
{
    /// <summary>
    /// 生成时间序 不重复 ID
    /// </summary>
    public static class IDHelper
    {
        static ConcurrentDictionary<long, int> Times = new ConcurrentDictionary<long, int>();
        /// <summary>
        /// 生成ID
        /// 时间戳13位，扩展4位为随机位，一共17位的 时间有序不重ID
        /// </summary>
        /// <returns></returns>
        public static long MarkID()
        {
            var now = GenerateTimestamp();
            var id = Times.AddOrUpdate(now, 0, (k, v) => v + 1);
            if (Times.Count > 5 * 60)
            {
                var removeids = Times.Keys.Where(k => k < now);
                foreach (var item in removeids)
                {
                    Times.TryRemove(item, out _);
                }
            }
            return now * 10000 + id;
        }
        public static long GenerateTimestamp()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        }
    }
}
