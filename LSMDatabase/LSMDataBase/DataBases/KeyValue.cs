using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase
{
    /// <summary>
    /// 数据信息 kv
    /// </summary>
    public class KeyValue
    {
        public string Key { get; set; }
        public byte[] DataValue { get; set; }
        public bool Deleted { get; set; }
        private object Value;
        public KeyValue() { }
        public KeyValue(string key, object value, bool Deleted = false)
        {
            Key = key;
            Value = value;
            DataValue = value.AsBytes();
            this.Deleted = Deleted;
        }
        public KeyValue(string key, byte[] dataValue, bool deleted)
        {
            Key = key;
            DataValue = dataValue;
            Deleted = deleted;
        }

        /// <summary>
        /// 是否存在有效数据,非删除状态
        /// </summary>
        /// <returns></returns>
        public bool IsSuccess()
        {
            return !Deleted || DataValue != null;
        }
        /// <summary>
        /// 值存不存在，无论删除还是不删除
        /// </summary>
        /// <returns></returns>
        public bool IsExist()
        {
            if (DataValue != null && !Deleted || DataValue == null && Deleted)
            {
                return true;
            }
            return false;
        }
        public T Get<T>() where T : class
        {
            if (Value == null)
            {
                Value = DataValue.AsObject<T>();
            }
            return (T)Value;
        }

        public static KeyValue Null = new KeyValue() { DataValue = null };
    }
}
