using StackExchange.Redis;

namespace NextAdmin.Redis;

/// <summary>
/// Redis service interface
/// </summary>
public interface IRedisService : IAsyncDisposable
{
    /// <summary>
    /// Get Redis connection
    /// </summary>
    IDatabase GetDatabase(int db = -1);

    /// <summary>
    /// Get Redis server
    /// </summary>
    IServer GetServer(string host, int port);

    /// <summary>
    /// Set string value
    /// </summary>
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);

    /// <summary>
    /// Get string value
    /// </summary>
    Task<string?> GetStringAsync(string key);

    /// <summary>
    /// Set object value
    /// </summary>
    Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    /// Get object value
    /// </summary>
    Task<T?> GetObjectAsync<T>(string key);

    /// <summary>
    /// Delete key
    /// </summary>
    Task<bool> DeleteAsync(string key);

    /// <summary>
    /// Check if key exists
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Set expiration time
    /// </summary>
    Task<bool> ExpireAsync(string key, TimeSpan? expiry);

    /// <summary>
    /// Get expiration time
    /// </summary>
    Task<TimeSpan?> GetExpiryAsync(string key);

    /// <summary>
    /// Get all keys
    /// </summary>
    Task<string[]> GetAllKeysAsync(string pattern = "*");

    /// <summary>
    /// Flush current database
    /// </summary>
    Task FlushDatabaseAsync();

    /// <summary>
    /// Get server information
    /// </summary>
    Task<Dictionary<string, string>> GetServerInfoAsync();

    /// <summary>
    /// Sync distributed cache with Redis
    /// </summary>
    Task SyncDistributedCacheAsync(string key);

    /// <summary>
    /// Batch sync distributed cache with Redis
    /// </summary>
    Task SyncDistributedCacheBatchAsync(string[] keys);

    /// <summary>
    /// Set hash field value
    /// </summary>
    Task<bool> SetHashAsync(string key, string field, string value);

    /// <summary>
    /// Get hash field value
    /// </summary>
    Task<string?> GetHashAsync(string key, string field);

    /// <summary>
    /// Delete hash field
    /// </summary>
    Task<bool> DeleteHashFieldAsync(string key, string field);

    /// <summary>
    /// Set key expiration time
    /// </summary>
    Task<bool> SetExpiryAsync(string key, TimeSpan expiry);
} 
