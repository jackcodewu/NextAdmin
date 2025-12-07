using StackExchange.Redis;

namespace NextAdmin.Redis;

/// <summary>
/// Redis服务接口
/// </summary>
public interface IRedisService : IAsyncDisposable
{
    /// <summary>
    /// 获取Redis连接
    /// </summary>
    IDatabase GetDatabase(int db = -1);

    /// <summary>
    /// 获取Redis服务器
    /// </summary>
    IServer GetServer(string host, int port);

    /// <summary>
    /// 设置字符串值
    /// </summary>
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);

    /// <summary>
    /// 获取字符串值
    /// </summary>
    Task<string?> GetStringAsync(string key);

    /// <summary>
    /// 设置对象值
    /// </summary>
    Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    /// 获取对象值
    /// </summary>
    Task<T?> GetObjectAsync<T>(string key);

    /// <summary>
    /// 删除键
    /// </summary>
    Task<bool> DeleteAsync(string key);

    /// <summary>
    /// 判断键是否存在
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// 设置过期时间
    /// </summary>
    Task<bool> ExpireAsync(string key, TimeSpan? expiry);

    /// <summary>
    /// 获取过期时间
    /// </summary>
    Task<TimeSpan?> GetExpiryAsync(string key);

    /// <summary>
    /// 获取所有键
    /// </summary>
    Task<string[]> GetAllKeysAsync(string pattern = "*");

    /// <summary>
    /// 清空当前数据库
    /// </summary>
    Task FlushDatabaseAsync();

    /// <summary>
    /// 获取服务器信息
    /// </summary>
    Task<Dictionary<string, string>> GetServerInfoAsync();

    /// <summary>
    /// 同步分布式缓存和Redis
    /// </summary>
    Task SyncDistributedCacheAsync(string key);

    /// <summary>
    /// 批量同步分布式缓存和Redis
    /// </summary>
    Task SyncDistributedCacheBatchAsync(string[] keys);

    /// <summary>
    /// 设置Hash字段值
    /// </summary>
    Task<bool> SetHashAsync(string key, string field, string value);

    /// <summary>
    /// 获取Hash字段值
    /// </summary>
    Task<string?> GetHashAsync(string key, string field);

    /// <summary>
    /// 删除Hash字段
    /// </summary>
    Task<bool> DeleteHashFieldAsync(string key, string field);

    /// <summary>
    /// 设置键的过期时间
    /// </summary>
    Task<bool> SetExpiryAsync(string key, TimeSpan expiry);
} 
