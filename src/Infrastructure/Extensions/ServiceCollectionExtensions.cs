using NextAdmin.Core.Domain.Interfaces;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Infrastructure.Configuration;
using NextAdmin.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using NextAdmin.Log;

namespace NextAdmin.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册MongoDB BSON类映射
            RegisterBsonClassMaps();

            // 配置设置（.NET 9 风格）
            services.AddOptions<MongoDbSettings>()
                .BindConfiguration(MongoDbSettings.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // 注册 MongoDB 客户端，使用 IOptions<MongoDbSettings>
            services.AddSingleton<IMongoClient>(sp =>
            {
                // 从配置获取 MongoDB 设置
                var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                    throw new ArgumentNullException(nameof(settings.ConnectionString), "MongoDB ConnectionString is not configured.");
                return new MongoClient(settings.ConnectionString);
            });

            services.AddScoped<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                if (string.IsNullOrWhiteSpace(settings.DatabaseName))
                    throw new ArgumentNullException(nameof(settings.DatabaseName), "MongoDB DatabaseName is not configured.");
                return client.GetDatabase(settings.DatabaseName);
            });

            // 🚀 自动注册所有仓储（扫描继承 AggregateRoot 的实体）
            // 方式1：标准注册（需要手动创建仓储类）
            // services.AddAutoRepositories();
            
            // 方式2：动态生成注册（运行时自动生成缺失的仓储类）
            services.AddAutoRepositoriesWithDynamicGeneration();
            
            // 打印已注册的仓储列表（开发环境可选）
            #if DEBUG
            services.PrintRegisteredRepositories();
            #endif
            
            return services;
        }

        /// <summary>
        /// 注册所有仓储服务（已废弃，使用自动注册）
        /// </summary>
        [Obsolete("请使用 AddAutoRepositories() 自动注册仓储")]
        private static void RegisterRepositories(IServiceCollection services)
        {
            // ⚠️ 此方法已废弃，现在使用自动注册机制
            // 自动注册会扫描所有继承 AggregateRoot 的实体类
            // 并自动注册对应的 I{Entity}Repository 和 {Entity}Repository
            
            // 如需手动覆盖某个仓储的注册，请使用：
            // services.AddRepository<IMenuRepository, MenuRepository>();
            
            // 通用仓储会自动注册，无需手动添加
            // services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        }

        /// <summary>
        /// 注册MongoDB BSON类映射
        /// </summary>
        private static void RegisterBsonClassMaps()
        {
            // 注册全局约定：忽略所有类的额外元素
            var conventionPack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            // 注册BaseEntity类映射
            if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
            {
                BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }

            // 注册AggregateRoot类映射
            if (!BsonClassMap.IsClassMapRegistered(typeof(AggregateRoot)))
            {
                BsonClassMap.RegisterClassMap<AggregateRoot>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }

            // 注意：VJSF 和其他业务特定的类映射已被移除
            // 如需添加业务特定的 BSON 类映射，请在此处添加
        }

        /// <summary>
        /// 执行数据库迁移
        /// </summary>
        /// <param name="database">MongoDB数据库实例</param>
        public static async Task ExecuteMigrationsAsync(IMongoDatabase database)
        {
            await DatabaseMigrationManager.ExecuteMigrationsAsync(database);
        }
    }
} 
