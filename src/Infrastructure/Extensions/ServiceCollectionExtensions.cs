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
            // Register MongoDB BSON class mappings
            RegisterBsonClassMaps();

            // Configure settings (.NET 9 style)
            services.AddOptions<MongoDbSettings>()
                .BindConfiguration(MongoDbSettings.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register MongoDB client, using IOptions<MongoDbSettings>
            services.AddSingleton<IMongoClient>(sp =>
            {
                // Get MongoDB settings from configuration
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

            // 🚀 Auto-register all repositories (scan entities inheriting AggregateRoot)
            // Method 1: Standard registration (requires manually creating repository classes)
            // services.AddAutoRepositories();
            
            // Method 2: Dynamic generation registration (automatically generates missing repository classes at runtime)
            services.AddAutoRepositoriesWithDynamicGeneration();
            
            // Print registered repository list (optional for development environment)
            #if DEBUG
            services.PrintRegisteredRepositories();
            #endif
            
            return services;
        }

        /// <summary>
        /// Register all repository services (obsolete, use auto-registration)
        /// </summary>
        [Obsolete("Please use AddAutoRepositories() for auto-registration")]
        private static void RegisterRepositories(IServiceCollection services)
        {
            // ⚠️ This method is obsolete, now use auto-registration mechanism
            // Auto-registration scans all entity classes inheriting AggregateRoot
            // and automatically registers corresponding I{Entity}Repository and {Entity}Repository
            
            // To manually override the registration of a specific repository, use:
            // services.AddRepository<IMenuRepository, MenuRepository>();
            
            // Generic repository is automatically registered, no need to add manually
            // services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        }

        /// <summary>
        /// Register MongoDB BSON class mappings
        /// </summary>
        private static void RegisterBsonClassMaps()
        {
            // Register global convention: Ignore extra elements for all classes
            var conventionPack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            // Register BaseEntity class mapping
            if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
            {
                BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }

            // Register AggregateRoot class mapping
            if (!BsonClassMap.IsClassMapRegistered(typeof(AggregateRoot)))
            {
                BsonClassMap.RegisterClassMap<AggregateRoot>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }

            // Note: VJSF and other business-specific class mappings have been removed
            // To add business-specific BSON class mappings, please add them here
        }

        /// <summary>
        /// Execute database migrations
        /// </summary>
        /// <param name="database">MongoDB database instance</param>
        public static async Task ExecuteMigrationsAsync(IMongoDatabase database)
        {
            await DatabaseMigrationManager.ExecuteMigrationsAsync(database);
        }
    }
} 
