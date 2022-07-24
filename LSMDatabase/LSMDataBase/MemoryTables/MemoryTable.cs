using LSMDataBase.DataBases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase.MemoryTables
{
    /// <summary>
    /// 内存表
    /// </summary>
    public class MemoryTable : IMemoryTable
    {
        /// <summary>
        /// 从小到大排序的
        /// </summary>
        private SortedList<long, MemoryTableValue> dics { get; set; } = new();
        public IDataBaseConfig DataBaseConfig { get; private set; }
        public MemoryTableValue CurrentMemoryTable
        {
            get
            {
                return dics.Values.Where(t => t.Immutable == false).First();
            }
        }
        public MemoryTable(IDataBaseConfig DataBaseConfig)
        {
            this.DataBaseConfig = DataBaseConfig;
            var dic = new MemoryTableValue();
            dics.Add(dic.Time, dic);
        }

        public int GetCount()
        {
            return dics.Values.Sum(t => t.Dic.Count);
        }

        public IList<string> GetKeys()
        {
            return dics.Values.SelectMany(t => t.Dic.Keys).Distinct().ToList();
        }

        public (List<KeyValue> keyValues, List<long> times) GetKeyValues(bool Immutable)
        {
            List<MemoryTableValue> MemoryTableValues = new List<MemoryTableValue>();
            if (Immutable)
            {

                MemoryTableValues.AddRange(dics.Values.Reverse().Where(t => t.Immutable));
            }
            else
            {
                MemoryTableValues.AddRange(dics.Values.Reverse().Where(t => !t.Immutable));
            }
            var dic = new Dictionary<string, KeyValue>();
            foreach (var item in MemoryTableValues)
            {
                foreach (var dicObj in item.Dic)
                {
                    dic[dicObj.Key] = dicObj.Value;
                }
            }
            return (dic.Select(t => t.Value).ToList(), MemoryTableValues.Select(t => t.Time).ToList());
        }
        /// <summary>
        /// 搜索(从新到旧，从大到小)
        /// </summary>
        public KeyValue Search(string key)
        {
            foreach (var item in dics.Values.Reverse())
            {
                if (item.Dic.TryGetValue(key, out var dataBaseValue))
                {
                    return dataBaseValue;
                }
            }
            return KeyValue.Null;
        }

        public void Set(KeyValue keyValue)
        {
            CurrentMemoryTable.Dic[keyValue.Key] = keyValue;
            Check();
        }
        public void Delete(KeyValue KeyValue)
        {
            CurrentMemoryTable.Dic[KeyValue.Key] = KeyValue;
        }
        public void Check()
        {
            if (CurrentMemoryTable.Dic.Count() >= DataBaseConfig.MemoryTableCount)
            {
                var value = new MemoryTableValue();
                dics.Add(value.Time, value);
                CurrentMemoryTable.Immutable = true;
            }
        }
        public int GetImmutableTableCount()
        {
            return dics.Values.Where(t => t.Immutable).Count();
        }
        public void Swap(List<long> times)
        {
            foreach (var item in times)
            {
                dics[item].Dispose();
                dics.Remove(item);
            }
        }
        public void Clear()
        {
            dics.Clear();
        }
        public void Dispose()
        {
            Clear();
        }
    }
}
