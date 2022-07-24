using LSMDataBase;
using System.Diagnostics;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "LSMDataBase by 蓝创精英团队";
            //Test_Set();
            Test_Set_Get();
            //Test_Delete();
            //Test_Get(); 
            Console.ReadLine();
        }
        /// <summary>
        /// 获取文件夹的大小
        /// </summary>
        /// <returns>返回MB</returns>
        public static long GetDirSize(string path)
        {
            long size = 0;
            foreach (var item in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                size += new FileInfo(item).Length;
            }
            return size / 1024 / 1024;
        }
        public static void Test_Set()
        {
            DataBase dataBase = new DataBase(new DataBaseConfig() { DataDir = Path.Combine(AppContext.BaseDirectory, "TempDB") });

            var keys = new List<string>();
            for (int i = 0; i < 1 * 1000; i++)
            {
                var key = $"123{i}";
                dataBase.Set(key, i);
                keys.Add(key);
            }
            var list = dataBase.GetKeys();
            var cahji = keys.Except(list).ToList();
            Console.WriteLine($"差集{cahji.Count}");
        }
        public static void Test_Set_Get()
        {   
            DataBase dataBase = new DataBase(new DataBaseConfig() { DataDir = Path.Combine(AppContext.BaseDirectory, "TempDB") });
            Stopwatch stopwatch = Stopwatch.StartNew();
            int index = 0;
            var value = new TestValue() { Name = "by 蓝创精英团队", Age = 20, Value = "5465415567498498486768184978495645646546546544654654654654654564654654648964564154" };
            var testLength = 100 * 10000;
            for (int i = 0; i < testLength; i++)
            {
                var key = $"bbbbb{index}";
                value.Key = key;
                dataBase.Set(key, value);
                index++;
            }
            stopwatch.Stop();
            var size = GetDirSize(dataBase.DataBaseConfig.DataDir);
            Log.Info($"keyvalue 数据长度:{value.AsBytes().Length} 实际文件大小:{size} MB 插入{testLength}条数据 耗时:{stopwatch.ElapsedMilliseconds}毫秒 或 {stopwatch.Elapsed.TotalSeconds} 秒,平均每秒插入:{testLength / stopwatch.Elapsed.Seconds}条");

            Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var key1 = $"bbbbb{testLength - 1}";
                    var key2 = $"bbbbb0";
                    stopwatch.Restart();
                    var result = dataBase.Get($"bbbbb{testLength - 1}");
                    if (result.IsExist())
                    {
                        Console.WriteLine(result.Get<TestValue>().Name.ToString());
                    }
                    else
                    {
                        Console.WriteLine($"{key1} 不存在!");
                    }
                    stopwatch.Stop();
                    Log.Info($"查询key1:{key1} 耗时:{stopwatch.ElapsedMilliseconds} 毫秒");
                    stopwatch.Restart();
                    var result2 = dataBase.Get($"bbbbb{testLength - 1}");
                    if (result2.IsExist())
                    {
                        Console.WriteLine(result2.Get<TestValue>().Name.ToString());
                    }
                    else
                    {
                        Console.WriteLine($"{key2} 不存在!");
                    }
                    stopwatch.Stop();
                    Log.Info($"查询key2:{key2} 耗时:{stopwatch.ElapsedMilliseconds} 毫秒");
                    Thread.Sleep(1000);
                }
            });

        }
        public static void Test_Delete()
        {
            DataBase dataBase = new DataBase(new DataBaseConfig() { DataDir = Path.Combine(AppContext.BaseDirectory, "TempDB") });
            Stopwatch stopwatch = Stopwatch.StartNew();
            var list = dataBase.GetKeys();
            stopwatch.Stop();
            Log.Info($"查询Keys:{list.Count} 耗时:{stopwatch.ElapsedMilliseconds} 毫秒");
            list.Sort();
            var key1 = list.Last();
            stopwatch.Restart();
            var result = dataBase.Get(key1);
            stopwatch.Stop();
            Log.Info($"查询Key:{key1} 耗时:{stopwatch.ElapsedMilliseconds} 毫秒");
            if (result.IsSuccess())
            {
                Console.WriteLine(result.Get<TestValue>().Name.ToString());
            }
            else
            {
                Console.WriteLine($"{key1} 不存在!");
            }
            dataBase.Delete(key1);

            list = dataBase.GetKeys();
            Console.WriteLine($"剩下数据个数:{list.Count}");

            stopwatch.Restart();
            result = dataBase.Get(key1);
            stopwatch.Stop();
            Log.Info($"查询Key:{key1} 耗时:{stopwatch.ElapsedMilliseconds} 毫秒");
            if (result.IsSuccess())
            {
                Console.WriteLine(result.ToString());
            }
            else
            {
                Console.WriteLine($"{key1} 不存在!");
            }
        }
        public static void Test_Get()
        {
            DataBase dataBase = new DataBase(new DataBaseConfig() { DataDir = Path.Combine(AppContext.BaseDirectory, "TempDB") });
            Stopwatch stopwatch = Stopwatch.StartNew();
            var list = dataBase.GetKeys();
            stopwatch.Stop();
            Log.Info($"查询Keys:{list.Count} 耗时:{stopwatch.ElapsedMilliseconds} 毫秒");
            list.Sort();
            var key1 = list.Last();
            List<long> Times = new List<long>();
            for (int i = 0; i < 100; i++)
            {
                stopwatch.Restart();
                var result = dataBase.Get(key1);
                stopwatch.Stop();
                Times.Add(stopwatch.ElapsedMilliseconds);
                Log.Info($"查询Key:{key1} 耗时:{stopwatch.ElapsedMilliseconds} 毫秒");
                if (result.IsSuccess())
                {
                    Console.WriteLine(result.Get<TestValue>().Name.ToString());
                }
                else
                {
                    Console.WriteLine($"{key1} 不存在!");
                }
            }
            //平均每条耗时
            Console.WriteLine($"百条查询平均每条查询耗时:{Times.Sum()/100}毫秒");
        }
        public static void Test_Clear()
        {
            DataBase dataBase = new DataBase(new DataBaseConfig() { DataDir = Path.Combine(AppContext.BaseDirectory, "TempDB") });

            dataBase.Clear();
        }
    }
    public class TestValue
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Value { get; set; }
        public override string ToString()
        {
            return $"key:{Key} name :{Name} age:{Age} value:{Value}";
        }
    }
}