using NextAdmin.Application.Interfaces;
using NextAdmin.Application.Services;
using NextAdmin.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NextAdmin.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register Redis service
            services.AddRedis(configuration);

            // Register AutoMapper
            services.AddAutoMapper(cfg => {
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            });

            // Register HTTP client factory
            services.AddHttpClient();

            // 注册MediatR
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            // 🚀 自动注册所有应用服务(扫描继承 AggregateRoot 的实体)
            services.AddAutoAppServices(generatePartialClasses: true);

            // 手动注册特殊服务（不遵循自动注册规则的服务）
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICaptchaService, CaptchaService>();
            services.AddScoped<DatabaseMigrationService>();
            services.AddScoped<DataSeederService>();

#if DEBUG
            // 调试模式下打印注册信息
            services.PrintRegisteredAppServices();
#endif

            return services;
        }

    }
} 
