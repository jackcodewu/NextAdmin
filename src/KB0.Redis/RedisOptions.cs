namespace NextAdmin.Redis;

/// <summary>
/// Redis配置选项
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Redis服务器地址
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// 默认数据库索引
    /// </summary>
    public int DefaultDatabase { get; set; }

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// 同步超时时间（毫秒）
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>
    /// 命令超时时间（毫秒）
    /// </summary>
    public int CommandTimeout { get; set; } = 5000;

    /// <summary>
    /// 异步超时时间（毫秒）
    /// </summary>
    public int AsyncTimeout { get; set; } = 5000;


    public TimeSpan DefaultExpiry { get; set; }=TimeSpan.FromMinutes(30);

    /// <summary>
    /// 连接池大小
    /// </summary>
    public int PoolSize { get; set; } = 50;

    /// <summary>
    /// 配置检查间隔（秒）
    /// </summary>
    public int ConfigCheckSeconds { get; set; } = 60;

    /// <summary>
    /// 是否启用SSL
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 客户端名称
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Redis版本
    /// </summary>
    public string Version { get; set; } = "6.0";
    public int ResponseTimeout { get; internal set; }
} 
