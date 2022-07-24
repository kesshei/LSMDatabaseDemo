#【万字长文】使用 LSM-Tree 思想基于.Net 6.0 C# 实现 KV 数据库（案例版）
>文章有点长，耐心看完应该可以懂实际原理到底是啥子。

>这是一个KV数据库的C#实现，目前用.NET 6.0实现的，目前算是属于雏形，骨架都已经完备，毕竟刚完工不到一星期。

>当然，这个其实也算是NoSQL的雏形，有助于深入了解相关数据库的内部原理概念，也有助于实际入门。

>适合对数据库原理以及实现感兴趣的朋友们。

>整体代码，大概1500行，核心代码大概500行。
# 为啥要实现一个数据库
大概2018年的时候，就萌生了想自己研发一个数据库的想法了，虽然，造轮子可能不如现有各种产品的强大，但是，能造者寥寥无几，而且，造数据库的书更是少的可怜，当然，不仅仅是造数据库的书少，而是各种各样高级的产品的创造级的书都少。

虽然，现在有各种各样的开源，但是，像我这种底子薄的，就不能轻易的了解，这些框架的架构设计，以及相关的理念，纯看代码，没个长时间，也不容易了解其存在的含义。

恰逢其时，前一个月看到【痴者工良】大佬的一篇《【万字长文】使用 LSM Tree 思想实现一个 KV 数据库 》文章给我很大触动，让我停滞的心，又砰砰跳了起来，虽然大佬是用GO语言实现的 ，但是，对我来讲，语言还是个问题么，只要技术思想一致，我完全可以用C#实现啊，也算是对【痴者工良】大佬的致敬，我这边紧随其后。

当然，我自己对数据的研究也是耗时很久，毕竟，研究什么都要先从原理开始研究，从谷歌三个论文《GFS，MapReduce，BigTable》开始，但是，论文，毕竟是论文，读不懂啊，又看了网上各种大佬的文章，还是很蒙蔽，实现的时候，也没人交流，导致各种流产。

有时候，自己实现某个产品框架的时候，总是在想，为啥BUG都让我处理一个遍哦，后来一想，你自己从新做个产品，也不能借鉴技术要点，那还不是从零开始，自然一一遇到BUG。

下图就是，我在想做数据库后，自己写写画画，但是，实际做的时候，逻辑表现总没有那么好，当然，这个是关系型数据库，难度比较高，下面可以看看之前的手稿，都是有想法了就画一下。

![](https://tupian.wanmeisys.com/markdown/1658653449452-4b61d26e-b167-4bdd-afc1-2c1e0786788a.png)

![](https://tupian.wanmeisys.com/markdown/1658653472526-d8b13566-1e72-43a1-b139-444ab4ae68e3.png)

![](https://tupian.wanmeisys.com/markdown/1658653510842-4a52fd33-8205-4bb3-947e-fc1761a248ab.png)

实现难度有点高，现在这个实现是KV数据库，算是列式数据库了，大名鼎鼎的HBase，底层数据库引擎就是LSM-Tree的技术思想。

# LSM-Tree 是啥子
>LSM-Tree 英文全称是 Log Structured Merge Tree （中文：日志结构合并树），是一种分层，有序，面向磁盘的数据结构，其核心思想是充分了利用了，磁盘批量的顺序写要远比随机写性能高的技术特点，来实现高写入吞吐量的存储系统的核心。

>具体的说，原理就是针对硬盘，尽量追加数据，而不是随机写数据，追加速度要比随机写的速度快，这种结构适合写多读少的场景，所以，LSM-Tree被设计来提供比传统的B+树或者ISAM更好的写操作吞吐量，通过消去随机的本地更新操作来达到这个性能目标。

>相关技术产品有Hbase、Cassandra、Leveldb、RocksDB、MongoDB、TiDB、Dynamodb、Cassandra 、Bookkeeper、SQLite  等

>所以，LSM-Tree的核心就是追加数据，而不是修改数据。

## LSM-Tree 架构分析
![](https://tupian.wanmeisys.com/markdown/1658579015747-2b2c93f8-b1e7-4564-9b46-b804f953bb43.png)

其实这个图已经表达了整体的设计思想了，主体其实就围绕着红色的线与黑色的线，两部分展开的，其中红色是写，黑色是读，箭头表示数据的方向，数字表示逻辑顺序。

整体包含大致三个部分，数据库操作部分（主要为读和写），内存部分(缓存表和不变缓存表)以及硬盘部分(WAL Log 和 SSTable)，这三个部分。

先对关键词解释一下

### MemoryTable
>内存表，一种临时缓存性质的数据表，可以用 二叉排序树实现，也可以用字典来实现，我这边是用字典实现的。

### WAL Log
>WAL 英文 (Write Ahead LOG) 是一种预写日志，用于在系统故障期间提供数据的持久性，这意味着当写入请求到来时，数据首先添加到 WAL 文件（有时称为日志）并刷新到更新内存数据结构之前的磁盘。

>如果用过Mysql，应该就知道BinLog文件，它们是一个道理，先写入到WAL Log里，记录起来，然后，写入到内存表，如果电脑突然死机了，内存里的东西肯定丢失了，那么，下一次重启，就从WAL Log 记录表里，从新恢复数据到当前的数据状态。

### Immutable MemoryTable
>Immutable(不变的)，相对于内存表来讲，它是不能写入新数据，是只读的。

### SSTable
>SSTable 英文 (Sorted Strings Table) ，有序字符串表，就是有序的字符串列表，使用它的好处是可以实现稀疏索引的效果，而且，合并文件更为简单方便，我要查某个Key，但是，它是基于 某个有序Key之间的，可以直接去文件里查，而不用都保存到内存里。

>这里我是用哈希表实现的，我认为浪费一点内存是值得的，毕竟为了快，浪费点空间是值得的，所以，目前是全索引加载到内存，而数据保存在SSTable里，当然，如果是为了更好的设计，也可以自己去实现有序表来用二分查找。

>我这个方便实现了之后，内存会加载大量的索引，相对来讲是快的，但是，内存会大一些，空间换时间的方案。

下面开始具体的流程分析

### LSM-Tree Write 路线分析
看下图，数据写入分析

![](https://tupian.wanmeisys.com/markdown/1658579759635-10d42efc-b1f1-44c6-92b0-e0062091a97c.png)

跟着红色线走，关注我从此不迷路。

#### LSM-Tree Write 路线分析第一步

![](https://tupian.wanmeisys.com/markdown/1658580069055-fcc12009-2eaa-4d66-b201-c7b3bd44e3bd.png)

第一步，只有两个部分需要注意的部分，分别是内存表和WAL.Log

写入数据先存储内存表，是为了快速的存储到数据库数据。

存储到WAL.Log，是为了防止异常情况下数据丢失。

正常情况下，写入到WAL.Log一份，然后，会写入到内存一份。

当程序崩溃了，或者，电脑断电异常了，重复服务后，就会先加载WAL.Log，按照从头到尾的顺序，恢复数据到内存表，直至结束，恢复到WAL.Log最后的状态，也就是内存表数据最后的状态。

##### 注
这里要注意的是，当后面的不变表(Immutable MemoryTable)写入到SSTable的时候，会清空WAL.Log文件，并同时把内存表的数据直接写入到WAL.log表中。

#### LSM-Tree Write 路线分析第二步

![](https://tupian.wanmeisys.com/markdown/1658581897208-019df8c7-f9cc-43ab-b180-d91ac4b99e61.png)

第二步，比较简单，就是在内存表count大于一定数的时候，就新增一个内存表的同时， 把它变为 Immutable MemoryTable （不变表），等待SSTable的落盘操作，这个时候，Immutable MemoryTable会有多个表存在。

#### LSM-Tree Write 路线分析第三步

![](https://tupian.wanmeisys.com/markdown/1658582217636-0cfbafc7-f2ea-4d4d-9397-76f4d1f785e3.png)

第三步，就是数据库会定时检查 Immutable MemoryTable （不变表）不变表是否存在，如果存在，就会直接落盘为SSTable表，不论当前内存里有多少 Immutable MemoryTable （不变表）。

默认从内存落盘的第一级SSTable都是 Level 0，然后，内置了当前的时间，所以是两级排序，先分级别，然后，分时间。

#### LSM-Tree Write 路线分析第四步

![](https://tupian.wanmeisys.com/markdown/1658582498763-054cd670-ae6f-4103-9466-e5df5f39232b.png)

第四步，其实就是段合并或者级合并压缩，就是判断 level0 这一个级别的所有 SSTable文件(SSTable0，SSTable1，SSTable2)，判断它们的总大小或者判断它们的总个数来判断，它们需不需要进行合并。

其中 Level 0 的大小如果是10M，那么 ,Level 1的大小就是 100M，依此类推。

当Level0的所有SSTable文件超过了10M，或者限定的大小，就会从按照WAL.Log的顺序思路，重新合并为一个大文件，先老数据再新数据这样遍历合并，如果已经删除的，则直接剔除在外，只保留最新状态。

如果 Level1的（全部SSTable）大小 超过100M，那么，触发Level1的收缩动作，执行过程跟Level0一样的操作，只是级别不同。

这样压缩的好处是使数据尽可能让文件量尽可能的少，毕竟，文件多，管理就不是很方便。

至此，写入路线已经分析完毕

#### 注
查询的时候，要先新数据，后旧数据，而分段合并压缩的时候，要先老数据垫底，新数据刷状态，这个是实现的时候需要注意的点。

### LSM-Tree Read 路线分析

![](https://tupian.wanmeisys.com/markdown/1658583466546-3233dc70-8f2f-4e17-9add-135c45b9c2db.png)

这就是数据的查找过程，跟着黑线和数字标记，很容易就看到了其访问顺序 

1. MemoryTable (内存表)
2. Immutable MemoryTable (不变表)
3. Level 0-N (SSTableN-SSTable1-SSTable0) (有序字符串表)

基本上来说就这三部分，而级别表是从0级开始往下找的，而每级内部的SSTable是从新到旧开始找的，找到就返回，不论key是删除还是正常的状态。

## LSM-Tree 架构分析与实现
![](https://tupian.wanmeisys.com/markdown/1658579015747-2b2c93f8-b1e7-4564-9b46-b804f953bb43.png)

核心思想：

其实就是一个时间有序的记录表，会记录每个操作，相当于是一个消息队列，记录一系列的动作，然后，回放动作，就获取到了最新的数据状态，也类似CQRS中的Event Store（事件存储），概念是相同的，那么实现的时候，就明白是一个什么本质。

Wal.log和SSTable，都是为了保证数据能落地持久化不丢失，而MemoryTable，偏向临时缓存的概念，当然，也有为了加速访问的作用。

所以，从这几个点来看，就分为了以下几个大的对象

1. Database 数据库( 起到对Wal.log，SSTable和MemoryTable 的管理职责)
2. Wal.log(记录临时数据日志)
3. MemoryTable(记录数据到内存，同时为数据库查找功能提供接口服务)
4. SSTable(管理SSTable文件，并提供SSTable的查询功能)

所以，针对这几个对象来设计相关的类接口设计。

### KeyValue (具体数据的结构)
设计的时候，要先设计实际数据的结构，我是这样设计的

主要有三个主要的信息，key, DataValue，Deleted ，其中DataValue是Object类型的，我这边写入到文件里的话，是直接序列化写入的。

```csharp
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
```

###  IDataBase (数据库接口)
主要对外交互用的主体类，数据库类，增删改查接口，都用 get,set,delete 表现。

```csharp
/// <summary>
/// 数据库接口
/// </summary>
public interface IDataBase : IDisposable
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    IDataBaseConfig DataBaseConfig { get; }
    /// <summary>
    /// 获取数据
    /// </summary>
    KeyValue Get(string key);
    /// <summary>
    /// 保存数据(或者更新数据)
    /// </summary>
    bool Set(KeyValue keyValue);
    /// <summary>
    /// 保存数据(或者更新数据)
    /// </summary>
    bool Set(string key, object value);
    /// <summary>
    /// 获取全部key
    /// </summary>
    List<string> GetKeys();
    /// <summary>
    /// 删除指定数据，并返回存在的数据
    /// </summary>
    KeyValue DeleteAndGet(string key);
    /// <summary>
    /// 删除数据
    /// </summary>
    void Delete(string key);
    /// <summary>
    /// 定时检查
    /// </summary>
    void Check(object state);
    /// <summary>
    /// 清除数据库所有数据
    /// </summary>
    void Clear();
}
```

####  IDataBase.Check (定期检查)
这个是定期检查Immutable MemoryTable(不变表)的定时操作，主要依赖IDataBaseConfig.CheckInterval 参数配置其触发间隔。

它的职责是检查内存表和检查SSTable 是否触发分段合并压缩的操作。
```csharp
public void Check(object state)
{
    //Log.Info($"定时心跳检查!");
    if (IsProcess)
    {
        return;
    }
    if (ClearState)
    {
        return;
    }
    try
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        IsProcess = true;
        checkMemory();
        TableManage.Check();
        stopwatch.Stop();
        GC.Collect();
        Log.Info($"定时心跳处理耗时:{stopwatch.ElapsedMilliseconds}毫秒");
    }
    finally
    {
        IsProcess = false;
    }
}
```

#### IDataBaseConfig (数据库配置文件)
数据库的配置文件，数据库保存在哪里，以及生成SSTable时的阈值配置，还有检测间隔时间配置。

```csharp
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
```

###  IMemoryTable (内存表)
![](https://tupian.wanmeisys.com/markdown/1658646969313-0771ffef-e158-48c0-bb4f-be17794db324.png)

这个表其实算是对内存数据的管理表了，主要是管理 MemoryTableValue 对象，这个对象是通过哈希字典来实现的，当然，你也可以选择其他结构，比如有序二叉树等。

```csharp
/// <summary>
/// 内存表(排序树，二叉树)
/// </summary>
public interface IMemoryTable : IDisposable
{
    IDataBaseConfig DataBaseConfig { get; }
    /// <summary>
    /// 获取总数
    /// </summary>
    int GetCount();
    /// <summary>
    /// 搜索(从新到旧，从大到小)
    /// </summary>
    KeyValue Search(string key);
    /// <summary>
    /// 设置新值
    /// </summary>
    void Set(KeyValue keyValue);
    /// <summary>
    /// 删除key
    /// </summary>
    void Delete(KeyValue keyValue);
    /// <summary>
    /// 获取所有 key 数据列表
    /// </summary>
    /// <returns></returns>
    IList<string> GetKeys();
    /// <summary>
    /// 获取所有数据
    /// </summary>
    /// <returns></returns>
    (List<KeyValue> keyValues, List<long> times) GetKeyValues(bool Immutable);
    /// <summary>
    /// 获取不变表的数量
    /// </summary>
    /// <returns></returns>
    int GetImmutableTableCount();
    /// <summary>
    /// 开始交换
    /// </summary>
    void Swap(List<long> times);
    /// <summary>
    /// 清空全部数据
    /// </summary>
    void Clear();
}
```


#### MemoryTableValue (对象的实现)
主要是通过 Immutable 这个属性实现了对不可变内存表的标记，具体实现是通过判断 IDataBaseConfig.MemoryTableCount (内存表的 kv 最大数量)来实现标记的。
```csharp
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
```

####  什么时机表状态转换为 Immutable MemoryTable(不变表)的
我这里实现的是从Set的入口处实现的，如果数目大于IDataBaseConfig.MemoryTableCount (内存表的 kv 最大数量)就改变其状态
```csharp
public void Check()
{
    if (CurrentMemoryTable.Dic.Count() >= DataBaseConfig.MemoryTableCount)
    {
        var value = new MemoryTableValue();
        dics.Add(value.Time, value);
        CurrentMemoryTable.Immutable = true;
    }
}
```
### IWalLog 
wallog，就简单许多，就直接把KeyValue 写入到文件即可，为了保证WalLog的持续写，所以，对象内部保留了此文件的句柄。而SSTable，就没有必要了，随时读。

```csharp
/// <summary>
/// 日志
/// </summary>
public interface IWalLog : IDisposable
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    IDataBaseConfig DataBaseConfig { get; }
    /// <summary>
    /// 加载Wal日志到内存表
    /// </summary>
    /// <returns></returns>
    IMemoryTable LoadToMemory();
    /// <summary>
    /// 写日志
    /// </summary>
    void Write(KeyValue data);
    /// <summary>
    /// 写日志
    /// </summary>
    void Write(List<KeyValue> data);
    /// <summary>
    /// 重置日志文件
    /// </summary>
    void Reset();
}
```

### ITableManage (SSTable表的管理)
为了更好的管理SSTable，需要有一个管理层，这个接口就是它的管理层，其中SSTable会有多层，每次用 Level+时间戳+db 作为文件名，用作外部识别。

```csharp
/// <summary>
/// 表管理项
/// </summary>
public interface ITableManage : IDisposable
{
    IDataBaseConfig DataBaseConfig { get; }
    /// <summary>
    /// 搜索(从新到老,从大到小)
    /// </summary>
    KeyValue Search(string key);
    /// <summary>
    /// 获取全部key
    /// </summary>
    List<string> GetKeys();
    /// <summary>
    /// 检查数据库文件，如果文件无效数据太多，就会触发整合文件
    /// </summary>
    void Check();
    /// <summary>
    /// 创建一个新Table
    /// </summary>
    void CreateNewTable(List<KeyValue> values, int Level = 0);
    /// <summary>
    /// 清理某个级别的数据
    /// </summary>
    /// <param name="Level"></param>
    public void Remove(int Level);
    /// <summary>
    /// 清除数据
    /// </summary>
    public void Clear();
}
```

#### ISSTable(SSTable 文件)
SSTable的内容管理，应该就是LSM-Tree的核心了，数据的合并，以及数据的查询，写入，加载，都是偏底层的操作，需要一丢丢的数据库知识。

```csharp
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
    public object ReadValue(DataPosition position);
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
```
#### IDataPosition(数据稀疏索引算是)
方便数据查询方便和方便从SSTable里读取到实际的数据内容。

```csharp
/// <summary>
/// 数据的位置
/// </summary>
public interface IDataPosition
{
    /// <summary>
    /// 索引起始位置
    /// </summary>
    public long IndexStart { get; set; }
    /// <summary>
    /// 开始地址
    /// </summary>
    public long Start { get; set; }
    /// <summary>
    /// 数据长度
    /// </summary>
    public long Length { get; set; }
    /// <summary>
    /// key的长度
    /// </summary>
    public long KeyLength { get; set; }
    /// <summary>
    /// 是否已经删除
    /// </summary>
    public bool Deleted { get; set; }
    public byte[] GetBytes();
}
```

### 数据结构分析
内部表的结构就不用说了，很简单，就是一个哈希字典，而有两个结构是要具体分析的，那就是 WALLog和SSTable文件。

#### WALLog 结构分析

![](https://tupian.wanmeisys.com/markdown/1658647325237-bc76c2e6-236b-441a-b273-c022320f91f4.png)

这个图横向不好画，我画成竖向了，WalLog里面存储的就是时间序的KeyValue数据，当它加载到Memory Table的时候，其实就是按照我所标的数字顺序依次叠加到最后的状态的。

同理，SSTable 数据分段合并压缩的时候，其实是跟这个一个原理的。

#### SSTable 结构分析
![](https://tupian.wanmeisys.com/markdown/1658647487827-a8db55c6-e623-4c51-9702-49f3913765f7.png)

SSTable，它本身是一个文件 名字大致如下:  

0_16586442986880000.db

格式为  层级_时间戳.db  这样的方式搞的命名规则，为此我还搞了一个生成时间序不重复 ID的简单算法。

##### SSTable 数据区

![](https://tupian.wanmeisys.com/markdown/1658647688534-b990dc1e-52bd-4aba-81bb-7aaabe38aed8.png)

数据区就很简单，把KeyValue.DataValue直接ToJson 就可以了，然后，直接写文件。

##### SSTable 稀疏索引区

![](https://tupian.wanmeisys.com/markdown/1658647762265-14ae9511-22fe-4da6-8f55-de3b4ce6844d.png)

这个区是按照与数据区对应的key的顺序写入的，主要是把DataValue对应的开始地址和结束地址放入到这个数据区了，另外把key也写入进去了。

好处是为了，当此SSTable加载索引(IDataPosition)到内存，省的把数据区的内容也加载进去，查找就方便许多，这也是索引的作用。

##### 元数据区

![](https://tupian.wanmeisys.com/markdown/1658647926734-1f8d80ca-886b-4b95-b20d-272b734624f5.png)

这个按照协议来讲，属于协议头，但是为啥放最后面呢，其实是为了计算方便，这也算是一个小妙招。

其中不仅包含了数据区的开始和结束，稀疏索引区的开始和结束，还包含了，此SSTable的版本和创建时间，以及当前SSTable所在的级别。


### SSTable 分段合并压缩
刚看这段功能逻辑的时候，脑子是懵的，使劲看了好久，分析了好久，还是把它写出来了，刚开始不理解，后来理解了，写着就容易许多了。

看下图:

![](https://tupian.wanmeisys.com/markdown/1658648202311-88f783ff-cb16-4fa9-84ca-bca3d09cd9c6.png)

其实合并是有状态的，这个就是中间态，我把他放到了图中间，然后，用白色的虚框表示。

整体逻辑就是，先从内存中定时把不变表生成为0级的SSTable，然后，0级就会有许多文件，如果这些文件大小超过了阈值，就合并此级的文件为一个大文件，按照WalLog的合并原理，然后把信息重新写入到本地为1级SSTable即可。

以此类推。

下面一个动图说明其合并效果。

![](https://tupian.wanmeisys.com/markdown/1658650539873-75951f90-6a85-4179-a850-3835762aef46.gif)

这个动图也说明一些事情，有此图，估计对原理就会多懂一些。


## LSMDatabase 性能测试
目前我这边测试用例都挺简单，如果有bug，就直接改了。
我这边测试是，直接写入一百万条数据，测试结果如下:


keyvalue 数据长度:151 实际文件大小:217 MB 插入1000000条数据 耗时:79320毫秒 或79.3207623秒,平均每秒插入:52631条

keyvalue 数据长度:151 实际文件大小:221 MB 插入1000000条数据 耗时:27561毫秒 或 27.5616519 秒,平均每秒插入:37037条

>1. keyvalue 数据长度:176 
2. 实际文件大小:215 MB 
3. 插入1000000条数据 耗时:29545毫秒 或 29.5457999 秒,
4. 平均每秒插入:34482条 或 30373 等( 配置不一样，环境不一样，会有不同，但是大致差不多)
5. 多次插入数据长度不同，配置不同，插入速度都会受到影响

>加载215 MB  1000000条数据条数据  耗时:2322 毫秒，也就是2秒(加载SSTable)

>内存稳定后占用500MB左右。

>稳定查询耗时: 百条查询平均每条查询耗时: 0毫秒。可能是因为用了字典的缘故，查询速度会快点，但是，特别点查询会有0.300左右的耗时个别现象。

>查询keys，一百万条耗时3秒，这个有点耗时，应该是数据量太大了。


![](https://tupian.wanmeisys.com/markdown/1658651704182-5ba2b17c-3b10-438c-8eca-19454ede142d.png)

![](https://tupian.wanmeisys.com/markdown/1658652148661-014c17f2-ff76-4188-89ae-075ac45f9d23.png)

![](https://tupian.wanmeisys.com/markdown/1658651589809-0a6567d1-3ff2-444e-95fe-60e6eae5e4eb.png)

![](https://tupian.wanmeisys.com/markdown/1658651554539-5e6fadca-c1a0-4a47-b256-af4416696122.png)

至此，此项目已经结束，虽然，还没有经历过压力测试，但是，整体骨架和内容已经完备，可以根据具体情况修复完善。目前我这边是没啥子问题的。

## 总结
任何事情的开始都是艰难的，跨越时间的长河，一步一步的学习，才有了今天它的诞生，会了就是会了，那么，应对下一个相关问题就会容易许多，我对这样的壁垒称之为，知识的屏障。

一叶障目，还真是存在，如何突破，唯有好奇心，坚持下去，一点点挖掘。

## 参考资料
>【万字长文】使用 LSM Tree 思想实现一个 KV 数据库
> >https://www.cnblogs.com/whuanle/p/16297025.html

>肖汉松：《从0开始：500行代码实现 LSM 数据库》
>>https://mp.weixin.qq.com/s/kCpV0evSuISET7wGyB9Efg

> cstack : 让我们建立一个简单的数据库
>>https://cstack.github.io/db_tutorial/

>数据库内核杂谈 - 一小时实现一个基本功能的数据库
>>https://www.jianshu.com/p/76e5cb53c864

>谷歌三大论文 GFS，MapReduce，BigTable 中的GFS和BigTable

## 致谢名单
1. 痴者工良
2. 陶德

虽然与以上大佬没有太过深入的交流，毕竟咖位还是有点高的，但是，通过文章以及简单的交流中，让我对数据库的研究更深一步，甚至真实的搞出来了，再次感谢。

## 代码地址
https://github.com/kesshei/LSMDatabaseDemo.git
 
https://gitee.com/kesshei/LSMDatabaseDemo.git

# 阅
一键三连呦！，感谢大佬的支持，您的支持就是我的动力!

# 版权
蓝创精英团队（公众号同名，CSDN同名）





