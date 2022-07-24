using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase
{
    /// <summary>
    /// json序列化
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// 类对像转换成json格式
        /// </summary> 
        /// <returns></returns>
        public static string? Serialize(this object t)
        {
            if (t == null)
            {
                return null;
            }
            IsoDateTimeConverter timeFormat = new IsoDateTimeConverter();
            timeFormat.DateTimeFormat = "yyyy-MM-dd hh:mm:ss";
            return JsonConvert.SerializeObject(t, Formatting.Indented, timeFormat);
        }
        /// <summary>
        /// json格式转换
        /// </summary>
        public static T? Deserialize<T>(this string strJson) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(strJson);
            }
            catch (Exception)
            {
            }
            return default;
        }
        /// <summary>
        /// 类对像转换成byte[]格式
        /// </summary> 
        public static byte[] AsBytes(this object t)
        {
            if (t == null)
            {
                return Array.Empty<byte>();
            }
            var JsonValue = t.Serialize();
            if (JsonValue == null)
            {
                return null;
            }
            return Encoding.UTF8.GetBytes(JsonValue);
        }
        /// <summary>
        /// 字节转换为指定对象
        /// </summary>
        public static T AsObject<T>(this byte[] bytes) where T : class
        {
            if (bytes != null && bytes.Length > 0)
            {
                var jsonValue = Encoding.UTF8.GetString(bytes);
                if (!string.IsNullOrEmpty(jsonValue))
                {
                    return jsonValue.Deserialize<T>();
                }
            }
            return null;
        }
    }
}
