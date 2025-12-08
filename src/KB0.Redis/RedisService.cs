using NextAdmin.Log;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace NextAdmin.Redis;

/// <summary>
/// JSON converter for ObjectId
/// </summary>
public class ObjectIdJsonConverter : JsonConverter<ObjectId>
{
    public override ObjectId ReadJson(JsonReader reader, Type objectType, ObjectId existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return ObjectId.Empty;
            
        if (reader.TokenType != JsonToken.String)
            throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing ObjectId");

        string value = reader.Value?.ToString();
        if (string.IsNullOrEmpty(value))
            return ObjectId.Empty;

        return ObjectId.Parse(value);
    }

    public override void WriteJson(JsonWriter writer, ObjectId value, JsonSerializer serializer)
    {
        if (value == ObjectId.Empty)
            writer.WriteNull();
        else
            writer.WriteValue(value.ToString());
    }
}

/// <summary>
/// Redis service implementation with distributed caching support
/// </summary>
public class RedisService : IRedisService
{
    private readonly ConnectionMultiplexer _connection;
    private readonly RedisOptions _options;
    private readonly JsonSerializerSettings _jsonSetting;
    private readonly IMemoryCache _memoryCache;
    private const int MaxRetryCount = 3;
    private const int RetryDelayMs = 1000;

    /// <summary>
    /// Constructor
    /// </summary>
    public RedisService(IOptions<RedisOptions> options, IMemoryCache memoryCache)
    {
        _options = options.Value;
        _memoryCache = memoryCache;
        _jsonSetting = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new ObjectIdJsonConverter() }
        };
        var config = new ConfigurationOptions
        {
            EndPoints = { _options.ConnectionString },
            DefaultDatabase = _options.DefaultDatabase,
            ConnectTimeout = _options.ConnectTimeout,
            SyncTimeout = _options.SyncTimeout,
            Ssl = _options.UseSsl,
            Password = _options.Password,
            ClientName = _options.ClientName,
            AbortOnConnectFail = false,
            AllowAdmin = true,
            ConnectRetry = 3,
            KeepAlive = 60,
            ResponseTimeout = _options.CommandTimeout,
            AsyncTimeout = _options.AsyncTimeout,
            ConfigCheckSeconds = _options.ConfigCheckSeconds,
            TieBreaker = "",
            DefaultVersion = new Version(6, 0),
            // Add reconnection configuration
            ReconnectRetryPolicy = new ExponentialRetry(5000),
            // Add heartbeat detection
            HeartbeatInterval =TimeSpan.FromSeconds(300)
        };

        try
        {
            _connection = ConnectionMultiplexer.Connect(config);
            _connection.ConnectionFailed += (sender, e) =>
            {
                LogHelper.Error($"Redis connection failed: {e.Exception?.Message}", e.Exception);
            };
            _connection.ConnectionRestored += (sender, e) =>
            {
                LogHelper.Info("Redis connection restored");
            };
            _connection.ErrorMessage += (sender, e) =>
            {
                LogHelper.Error($"Redis error: {e.Message}");
            };
        }
        catch (Exception ex)
        {
            LogHelper.Error("Redis connection initialization failed", ex);
            throw;
        }
    }

    /// <summary>
    /// Execute Redis operation with retry mechanism
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        var retryCount = 0;
        while (retryCount < MaxRetryCount)
        {
            try
            {
                return await operation();
            }
            catch (RedisConnectionException ex)
            {
                retryCount++;
                if (retryCount >= MaxRetryCount)
                {
                    LogHelper.Error($"Redis operation failed after {MaxRetryCount} retries: {operationName}", ex);
                    throw;
                }
                LogHelper.Warn($"Redis operation failed, retry attempt {retryCount}: {operationName}");
                await Task.Delay(RetryDelayMs * retryCount);
            }
        }
        throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis operation failed, retry attempts exhausted");
    }

    /// <summary>
    /// Get Redis connection
    /// </summary>
    public IDatabase GetDatabase(int db = -1)
    {
        return _connection.GetDatabase(db);
    }

    /// <summary>
    /// Get Redis server
    /// </summary>
    public IServer GetServer(string host, int port)
    {
        return _connection.GetServer(host, port);
    }

    /// <summary>
    /// Set string value
    /// </summary>
    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        var actualExpiry = expiry ?? _options.DefaultExpiry;
        
        // Write to local memory cache
        _memoryCache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = actualExpiry
        });

        var redisResult = await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.StringSetAsync(key, value, actualExpiry);
        }, $"SetStringAsync({key})");
        LogHelper.Debug($"Set string value completed: {key}, Redis result: {redisResult}");
        return redisResult;
    }

    /// <summary>
    /// Get string value
    /// </summary>
    public async Task<string?> GetStringAsync(string key)
    {
        // Query distributed cache first
        if (_memoryCache.TryGetValue<string>(key, out var cachedValue) && !string.IsNullOrEmpty(cachedValue))
        {
            LogHelper.Debug($"Value retrieved from distributed cache: {key}");
            return cachedValue;
        }

        // Not in distributed cache, query Redis
        return await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            var value = await db.StringGetAsync(key);
            if (value.HasValue)
            {
                var stringValue = (string?)value;
                // Sync value from Redis to distributed cache
                _memoryCache.Set(key, stringValue, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.DefaultExpiry
                });
                LogHelper.Debug($"Value retrieved from Redis and synced to distributed cache: {key}");
                return stringValue;
            }
            return null;
        }, $"GetStringAsync({key})");
    }

    /// <summary>
    /// Set object value
    /// </summary>
    public async Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var actualExpiry = expiry ?? _options.DefaultExpiry;
        var json = JsonConvert.SerializeObject(value, _jsonSetting);
        
        // Set both distributed cache and Redis
        _memoryCache.Set(key, json, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = actualExpiry
        });

        var redisResult = await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.StringSetAsync(key, json, actualExpiry);
        }, $"SetObjectAsync({key})");
        LogHelper.Debug($"Set object value completed: {key}, Redis result: {redisResult}");
        return redisResult;
    }

    /// <summary>
    /// Get object value
    /// </summary>
    public async Task<T?> GetObjectAsync<T>(string key)
    {
        // Query distributed cache first
        if (_memoryCache.TryGetValue<string>(key, out var cachedJson) && !string.IsNullOrEmpty(cachedJson))
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(cachedJson, _jsonSetting);
                LogHelper.Debug($"Object retrieved from distributed cache: {key}");
                return result;
            }
            catch (JsonSerializationException ex) when (ex.Message.Contains("ObjectId"))
            {
                // ObjectId type conversion error, clear memory and Redis cache
                LogHelper.Warn($"Failed to deserialize object from distributed cache (ObjectId type mismatch): {key}, clearing all caches");
                _memoryCache.Remove(key);
                try
                {
                    var db = GetDatabase();
                    await db.KeyDeleteAsync(key);
                    LogHelper.Info($"Cleared erroneous cache in Redis: {key}");
                }
                catch (Exception deleteEx)
                {
                    LogHelper.Error($"Failed to clear Redis cache: {key}", deleteEx);
                }
                return default;
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"Failed to deserialize object from distributed cache: {key}, error: {ex.Message}");
                _memoryCache.Remove(key);
            }
        }

        // Not in distributed cache or deserialization failed, query Redis
        return await ExecuteWithRetryAsync(async () =>
        {
            var json = await GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
                return default;

            try
            {
                var result = JsonConvert.DeserializeObject<T>(json, _jsonSetting);
                if (result != null)
                {
                    // Sync object from Redis to distributed cache
                    _memoryCache.Set(key, json, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _options.DefaultExpiry
                    });
                    LogHelper.Debug($"Object retrieved from Redis and synced to distributed cache: {key}");
                }
                return result;
            }
            catch (JsonSerializationException ex) when (ex.Message.Contains("ObjectId"))
            {
                // ObjectId type conversion error, clear memory and Redis cache
                LogHelper.Warn($"Failed to deserialize object from Redis (ObjectId type mismatch): {key}, clearing all caches");
                _memoryCache.Remove(key);
                var db = GetDatabase();
                await db.KeyDeleteAsync(key);
                LogHelper.Info($"Cleared erroneous cache in Redis: {key}");
                return default;
            }
        }, $"GetObjectAsync({key})");
    }

    /// <summary>
    /// Delete key
    /// </summary>
    public async Task<bool> DeleteAsync(string key)
    {
        // Delete from both distributed cache and Redis
        _memoryCache.Remove(key);

        var redisResult = await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.KeyDeleteAsync(key);
        }, $"DeleteAsync({key})");
        LogHelper.Debug($"Delete key completed: {key}, Redis result: {redisResult}");
        return redisResult;
    }

    /// <summary>
    /// Check if key exists
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        // Query distributed cache first
        if (_memoryCache.TryGetValue<string>(key, out var cachedValue) && !string.IsNullOrEmpty(cachedValue))
        {
            LogHelper.Debug($"Key confirmed to exist in distributed cache: {key}");
            return true;
        }

        // Not in distributed cache, query Redis
        return await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            var exists = await db.KeyExistsAsync(key);
            if (exists)
            {
                // If exists in Redis, try to sync to distributed cache
                try
                {
                    var value = await db.StringGetAsync(key);
                    if (value.HasValue)
                    {
                        _memoryCache.Set(key, value.ToString(), new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _options.DefaultExpiry
                        });
                        LogHelper.Debug($"Synced key from Redis to distributed cache: {key}");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Warn($"Failed to sync key to distributed cache: {key}, error: {ex.Message}");
                }
            }
            return exists;
        }, $"ExistsAsync({key})");
    }

    /// <summary>
    /// Set expiration time
    /// </summary>
    public async Task<bool> ExpireAsync(string key, TimeSpan? expiry)
    {
        // Set expiration time for both Redis and distributed cache
        if (expiry.HasValue)
        {
            try
            {
                if (_memoryCache.TryGetValue<string>(key, out var cachedValue))
                {
                    _memoryCache.Set(key, cachedValue, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiry.Value
                    });
                    LogHelper.Debug($"Updated distributed cache expiration time: {key}, expiry: {expiry.Value}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"Failed to update distributed cache expiration time: {key}, error: {ex.Message}");
            }
        }

        var redisResult = await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.KeyExpireAsync(key, expiry);
        }, $"ExpireAsync({key})");
        LogHelper.Debug($"Set expiration time completed: {key}, Redis result: {redisResult}");
        return redisResult;
    }

    /// <summary>
    /// Get expiration time
    /// </summary>
    public async Task<TimeSpan?> GetExpiryAsync(string key)
    {
        // Query distributed cache first
        if (_memoryCache.TryGetValue<string>(key, out var cachedValue) && !string.IsNullOrEmpty(cachedValue))
        {
            // Distributed cache exists, but cannot get expiration time directly, need to query Redis
            LogHelper.Debug($"Key confirmed to exist in distributed cache, querying Redis for expiration time: {key}");
        }

        // Query Redis to get expiration time
        return await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.KeyTimeToLiveAsync(key);
        }, $"GetExpiryAsync({key})");
    }

    /// <summary>
    /// Get all keys (supports wildcards, e.g. user:*, *:suffix, *contains*). Uses the configured default database.
    /// </summary>
    public Task<string[]> GetAllKeysAsync(string pattern = "*") => GetAllKeysAsync(pattern, _options.DefaultDatabase, 500);

    /// <summary>
    /// Get all keys (with database and page size). Note: Traversing keys is expensive, use cautiously in production at scale.
    /// </summary>
    /// <param name="pattern">Wildcard pattern (glob), e.g.: user:*, *:123, *order*, ? for single character, [ab] for set</param>
    /// <param name="db">Database number, defaults to the configured DefaultDatabase</param>
    /// <param name="pageSize">SCAN step size, default 500</param>
    public async Task<string[]> GetAllKeysAsync(string pattern, int? db, int pageSize = 500)
    {
        var database = db ?? _options.DefaultDatabase;
        return await ExecuteWithRetryAsync(async () =>
        {
            var parts = _options.ConnectionString.Split(':');
            var host = parts[0];
            var port = int.Parse(parts[1]);
            var server = GetServer(host, port);
            var keys = new List<string>();
            await foreach (var key in server.KeysAsync(database: database, pattern: pattern, pageSize: pageSize))
            {
                keys.Add(key.ToString());
            }
            return keys.ToArray();
        }, $"GetAllKeysAsync({pattern},{database},{pageSize})");
    }

    /// <summary>
    /// Flush current database
    /// </summary>
    public async Task FlushDatabaseAsync()
    {
        // Flush Redis database
        var flushRedisTask = ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            await db.ExecuteAsync("FLUSHDB");
            return true;
        }, "FlushDatabaseAsync");

        // Flush distributed cache (Note: distributed cache usually cannot clear all keys, just logging here)
        LogHelper.Info("Flushing Redis database, recommend manually clearing distributed cache");

        await flushRedisTask;
        LogHelper.Debug("Flush database completed");
    }

    /// <summary>
    /// Get server information
    /// </summary>
    public async Task<Dictionary<string, string>> GetServerInfoAsync()
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var server = GetServer(_options.ConnectionString.Split(':')[0], int.Parse(_options.ConnectionString.Split(':')[1]));
            var info = await server.InfoAsync();
            var result = new Dictionary<string, string>();
            
            foreach (var group in info)
            {
                foreach (var item in group)
                {
                    result[item.Key] = item.Value;
                }
            }
            
            return result;
        }, "GetServerInfoAsync");
    }

    /// <summary>
    /// Sync distributed cache with Redis
    /// </summary>
    public async Task SyncDistributedCacheAsync(string key)
    {
        try
        {
            // Get value from Redis
            var db = GetDatabase();
            var value = await db.StringGetAsync(key);
            if (value.HasValue)
            {
                // Sync to distributed cache
                _memoryCache.Set(key, value.ToString(), new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.DefaultExpiry
                });
                LogHelper.Debug($"Synced key to distributed cache: {key}");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"Failed to sync key to distributed cache: {key}, error: {ex.Message}");
        }
    }

    /// <summary>
    /// Batch sync distributed cache with Redis
    /// </summary>
    public async Task SyncDistributedCacheBatchAsync(string[] keys)
    {
        var tasks = keys.Select(key => SyncDistributedCacheAsync(key));
        await Task.WhenAll(tasks);
        LogHelper.Debug($"Batch sync completed, total {keys.Length} keys");
    }

    /// <summary>
    /// Set hash field value
    /// </summary>
    public async Task<bool> SetHashAsync(string key, string field, string value)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.HashSetAsync(key, field, value);
        }, $"SetHashAsync({key}, {field})");
    }

    /// <summary>
    /// Get hash field value
    /// </summary>
    public async Task<string?> GetHashAsync(string key, string field)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            var value = await db.HashGetAsync(key, field);
            return value.HasValue ? (string?)value : null;
        }, $"GetHashAsync({key}, {field})");
    }

    /// <summary>
    /// Delete hash field
    /// </summary>
    public async Task<bool> DeleteHashFieldAsync(string key, string field)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.HashDeleteAsync(key, field);
        }, $"DeleteHashFieldAsync({key}, {field})");
    }

    /// <summary>
    /// Set key expiration time
    /// </summary>
    public async Task<bool> SetExpiryAsync(string key, TimeSpan expiry)
    {
        return await ExpireAsync(key, expiry);
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
} 
