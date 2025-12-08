namespace NextAdmin.Redis;

/// <summary>
/// Redis configuration options
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Redis server address
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Default database index
    /// </summary>
    public int DefaultDatabase { get; set; }

    /// <summary>
    /// Connection timeout (milliseconds)
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// Sync timeout (milliseconds)
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>
    /// Command timeout (milliseconds)
    /// </summary>
    public int CommandTimeout { get; set; } = 5000;

    /// <summary>
    /// Async timeout (milliseconds)
    /// </summary>
    public int AsyncTimeout { get; set; } = 5000;


    public TimeSpan DefaultExpiry { get; set; }=TimeSpan.FromMinutes(30);

    /// <summary>
    /// Connection pool size
    /// </summary>
    public int PoolSize { get; set; } = 50;

    /// <summary>
    /// Config check interval (seconds)
    /// </summary>
    public int ConfigCheckSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to enable SSL
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Client name
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Redis version
    /// </summary>
    public string Version { get; set; } = "6.0";
    public int ResponseTimeout { get; internal set; }
} 
