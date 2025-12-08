using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NextAdmin.Redis;

/// <summary>
/// Service collection extension methods
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Redis service
    /// </summary>
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Redis").Get<RedisOptions>();
        if (options == null)
            throw new ArgumentNullException(nameof(options), "Redis configuration is missing");

        services.Configure<RedisOptions>(opt =>
        {
            opt.ConnectionString = options.ConnectionString;
            opt.DefaultDatabase = options.DefaultDatabase;
            opt.ConnectTimeout = options.ConnectTimeout;
            opt.SyncTimeout = options.SyncTimeout;
            opt.ResponseTimeout = options.ResponseTimeout;
            opt.UseSsl = options.UseSsl;
            opt.Password = options.Password;
            opt.ClientName = options.ClientName;
        });

        services.AddSingleton<IRedisService, RedisService>();
        return services;
    }

    /// <summary>
    /// Add Redis service
    /// </summary>
    public static IServiceCollection AddRedis(this IServiceCollection services, Action<RedisOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IRedisService, RedisService>();
        return services;
    }
} 
