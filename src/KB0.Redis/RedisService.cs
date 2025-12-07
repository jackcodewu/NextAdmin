using NextAdmin.Log;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace NextAdmin.Redis;

/// <summary>
/// ObjectId的JSON转换器
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
/// Redis服务实现类，支持分布式缓存
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
    /// 构造函数
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
            // 添加重连配置
            ReconnectRetryPolicy = new ExponentialRetry(5000),
            // 添加心跳检测
            HeartbeatInterval =TimeSpan.FromSeconds(300)
        };

        try
        {
            _connection = ConnectionMultiplexer.Connect(config);
            _connection.ConnectionFailed += (sender, e) =>
            {
                LogHelper.Error($"Redis连接失败: {e.Exception?.Message}", e.Exception);
            };
            _connection.ConnectionRestored += (sender, e) =>
            {
                LogHelper.Info("Redis连接已恢复");
            };
            _connection.ErrorMessage += (sender, e) =>
            {
                LogHelper.Error($"Redis错误: {e.Message}");
            };
        }
        catch (Exception ex)
        {
            LogHelper.Error("Redis连接初始化失败", ex);
            throw;
        }
    }

    /// <summary>
    /// 执行Redis操作，带重试机制
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
                    LogHelper.Error($"Redis操作失败，已重试{MaxRetryCount}次: {operationName}", ex);
                    throw;
                }
                LogHelper.Warn($"Redis操作失败，正在进行第{retryCount}次重试: {operationName}");
                await Task.Delay(RetryDelayMs * retryCount);
            }
        }
        throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis操作失败，重试次数已用完");
    }

    /// <summary>
    /// 获取Redis连接
    /// </summary>
    public IDatabase GetDatabase(int db = -1)
    {
        return _connection.GetDatabase(db);
    }

    /// <summary>
    /// 获取Redis服务器
    /// </summary>
    public IServer GetServer(string host, int port)
    {
        return _connection.GetServer(host, port);
    }

    /// <summary>
    /// 设置字符串值
    /// </summary>
    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        var actualExpiry = expiry ?? _options.DefaultExpiry;
        
        // 写入本地内存缓存
        _memoryCache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = actualExpiry
        });

        var redisResult = await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.StringSetAsync(key, value, actualExpiry);
        }, $"SetStringAsync({key})");
        LogHelper.Debug($"设置字符串值完成: {key}, Redis结果: {redisResult}");
        return redisResult;
    }

    /// <summary>
    /// 获取字符串值
    /// </summary>
    public async Task<string?> GetStringAsync(string key)
    {
        // 先查询分布式缓存
        if (_memoryCache.TryGetValue<string>(key, out var cachedValue) && !string.IsNullOrEmpty(cachedValue))
        {
            LogHelper.Debug($"从分布式缓存获取到值: {key}");
            return cachedValue;
        }

        // 分布式缓存中没有，查询Redis
        return await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            var value = await db.StringGetAsync(key);
            if (value.HasValue)
            {
                var stringValue = (string?)value;
                // 将Redis的值同步到分布式缓存
                _memoryCache.Set(key, stringValue, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.DefaultExpiry
                });
                LogHelper.Debug($"从Redis获取到值并同步到分布式缓存: {key}");
                return stringValue;
            }
            return null;
        }, $"GetStringAsync({key})");
    }

    /// <summary>
    /// 设置对象值
    /// </summary>
    public async Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var actualExpiry = expiry ?? _options.DefaultExpiry;
        var json = JsonConvert.SerializeObject(value, _jsonSetting);
        
        // 同时设置分布式缓存和Redis
        _memoryCache.Set(key, json, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = actualExpiry
        });

        var redisResult = await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.StringSetAsync(key, json, actualExpiry);
        }, $"SetObjectAsync({key})");
        LogHelper.Debug($"设置对象值完成: {key}, Redis结果: {redisResult}");
        return redisResult;
    }

    /// <summary>
    /// 获取对象值
    /// </summary>
    public async Task<T?> GetObjectAsync<T>(string key)
    {
        // 先查询分布式缓存
        if (_memoryCache.TryGetValue<string>(key, out var cachedJson) && !string.IsNullOrEmpty(cachedJson))
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(cachedJson, _jsonSetting);
                LogHelper.Debug($"从分布式缓存获取到对象: {key}");
                return result;
            }
            catch (JsonSerializationException ex) when (ex.Message.Contains("ObjectId"))
            {
                // ObjectId类型转换错误，清除内存和Redis缓存
                LogHelper.Warn($"从分布式缓存反序列化对象失败(ObjectId类型不匹配): {key}, 清除所有缓存");
                _memoryCache.Remove(key);
                try
                {
                    var db = GetDatabase();
                    await db.KeyDeleteAsync(key);
                    LogHelper.Info($"已清除Redis中的错误缓存: {key}");
                }
                catch (Exception deleteEx)
                {
                    LogHelper.Error($"清除Redis缓存失败: {key}", deleteEx);
                }
                return default;
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"从分布式缓存反序列化对象失败: {key}, 错误: {ex.Message}");
                _memoryCache.Remove(key);
            }
        }

        // 分布式缓存中没有或反序列化失败，查询Redis
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
                    // 将Redis的对象同步到分布式缓存
                    _memoryCache.Set(key, json, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _options.DefaultExpiry
                    });
                    LogHelper.Debug($"从Redis获取到对象并同步到分布式缓存: {key}");
                }
                return result;
            }
            catch (JsonSerializationException ex) when (ex.Message.Contains("ObjectId"))
            {
                // ObjectId类型转换错误，清除内存和Redis缓存
                LogHelper.Warn($"从Redis反序列化对象失败(ObjectId类型不匹配): {key}, 清除所有缓存");
                _memoryCache.Remove(key);
                var db = GetDatabase();
                await db.KeyDeleteAsync(key);
                LogHelper.Info($"已清除Redis中的错误缓存: {key}");
                return default;
            }
        }, $"GetObjectAsync({key})");
    }

    /// <summary>
    /// 删除键
    /// </summary>
    public async Task<bool> DeleteAsync(string key)
    {
        // 同时删除分布式缓存和Redis
        _memoryCache.Remove(key);

        var redisResult = await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.KeyDeleteAsync(key);
        }, $"DeleteAsync({key})");
        LogHelper.Debug($"删除键完成: {key}, Redis结果: {redisResult}");
        return redisResult;
    }

    /// <summary>
    /// 判断键是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        // 先查询分布式缓存
        if (_memoryCache.TryGetValue<string>(key, out var cachedValue) && !string.IsNullOrEmpty(cachedValue))
        {
            LogHelper.Debug($"从分布式缓存确认键存在: {key}");
            return true;
        }

        // 分布式缓存中没有，查询Redis
        return await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            var exists = await db.KeyExistsAsync(key);
            if (exists)
            {
                // 如果Redis中存在，尝试同步到分布式缓存
                try
                {
                    var value = await db.StringGetAsync(key);
                    if (value.HasValue)
                    {
                        _memoryCache.Set(key, value.ToString(), new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _options.DefaultExpiry
                        });
                        LogHelper.Debug($"从Redis同步键到分布式缓存: {key}");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Warn($"同步键到分布式缓存失败: {key}, 错误: {ex.Message}");
                }
            }
            return exists;
        }, $"ExistsAsync({key})");
    }

    /// <summary>
    /// 设置过期时间
    /// </summary>
    public async Task<bool> ExpireAsync(string key, TimeSpan? expiry)
    {
        // 同时设置Redis和分布式缓存的过期时间
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
                    LogHelper.Debug($"更新分布式缓存过期时间: {key}, 过期时间: {expiry.Value}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"更新分布式缓存过期时间失败: {key}, 错误: {ex.Message}");
            }
        }

        var redisResult = await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.KeyExpireAsync(key, expiry);
        }, $"ExpireAsync({key})");
        LogHelper.Debug($"设置过期时间完成: {key}, Redis结果: {redisResult}");
        return redisResult;
    }

    /// <summary>
    /// 获取过期时间
    /// </summary>
    public async Task<TimeSpan?> GetExpiryAsync(string key)
    {
        // 先查询分布式缓存
        if (_memoryCache.TryGetValue<string>(key, out var cachedValue) && !string.IsNullOrEmpty(cachedValue))
        {
            // 分布式缓存存在，但无法直接获取过期时间，需要查询Redis
            LogHelper.Debug($"从分布式缓存确认键存在，查询Redis获取过期时间: {key}");
        }

        // 查询Redis获取过期时间
        return await ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            return await db.KeyTimeToLiveAsync(key);
        }, $"GetExpiryAsync({key})");
    }

    /// <summary>
    /// 获取所有键（支持通配符，如 user:*、*:suffix、*contains*）。使用配置的默认数据库。
    /// </summary>
    public Task<string[]> GetAllKeysAsync(string pattern = "*") => GetAllKeysAsync(pattern, _options.DefaultDatabase, 500);

    /// <summary>
    /// 获取所有键（带数据库与分页大小）。注意：遍历键成本较高，谨慎在生产环境大规模使用。
    /// </summary>
    /// <param name="pattern">通配符模式（glob），例如：user:*, *:123, *order*, ? 替单字符, [ab] 集合</param>
    /// <param name="db">数据库编号，默认使用配置的 DefaultDatabase</param>
    /// <param name="pageSize">SCAN 步长，默认 500</param>
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
    /// 清空当前数据库
    /// </summary>
    public async Task FlushDatabaseAsync()
    {
        // 清空Redis数据库
        var flushRedisTask = ExecuteWithRetryAsync(async () =>
        {
            var db = GetDatabase();
            await db.ExecuteAsync("FLUSHDB");
            return true;
        }, "FlushDatabaseAsync");

        // 清空分布式缓存（注意：分布式缓存通常无法清空所有键，这里只是记录日志）
        LogHelper.Info("清空Redis数据库，建议手动清理分布式缓存");

        await flushRedisTask;
        LogHelper.Debug("清空数据库完成");
    }

    /// <summary>
    /// 获取服务器信息
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
    /// 同步分布式缓存和Redis
    /// </summary>
    public async Task SyncDistributedCacheAsync(string key)
    {
        try
        {
            // 从Redis获取值
            var db = GetDatabase();
            var value = await db.StringGetAsync(key);
            if (value.HasValue)
            {
                // 同步到分布式缓存
                _memoryCache.Set(key, value.ToString(), new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.DefaultExpiry
                });
                LogHelper.Debug($"同步键到分布式缓存: {key}");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"同步键到分布式缓存失败: {key}, 错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 批量同步分布式缓存和Redis
    /// </summary>
    public async Task SyncDistributedCacheBatchAsync(string[] keys)
    {
        var tasks = keys.Select(key => SyncDistributedCacheAsync(key));
        await Task.WhenAll(tasks);
        LogHelper.Debug($"批量同步完成，共{keys.Length}个键");
    }

    /// <summary>
    /// 设置Hash字段值
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
    /// 获取Hash字段值
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
    /// 删除Hash字段
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
    /// 设置键的过期时间
    /// </summary>
    public async Task<bool> SetExpiryAsync(string key, TimeSpan expiry)
    {
        return await ExpireAsync(key, expiry);
    }

    /// <summary>
    /// 释放资源
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
