using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMDataBase
{
    public class Log
    {
        public static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        public static object Lock = new object();
        static Log()
        {
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
        }
        public static void Info(string msg)
        {
            var MSG = $"{DateTime.Now} - msg: {msg}";
            Console.WriteLine(MSG);
            Write(MSG);
        }
        public static void Error(Exception ex, string msg)
        {
            var MSG = $"{DateTime.Now}  - Exception: {ex.Message + ex.StackTrace} - msg: {msg}";
            Console.WriteLine(MSG);
            Write(MSG);
        }
        private static void Write(string msg)
        {
            lock (Lock)
            {
                File.AppendAllLines(Path.Combine(LogPath, $"{DateTime.Now.ToString("yyyyMMdd")}_log.txt"), new List<string>() { msg });
            }
        }
    }
}
