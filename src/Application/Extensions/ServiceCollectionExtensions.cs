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

            // Register MediatR
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            // 🚀 Auto-register all application services (scan entities inheriting AggregateRoot)
            services.AddAutoAppServices(generatePartialClasses: true);

            // Manually register special services (services that don't follow auto-registration rules)
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICaptchaService, CaptchaService>();
        
            services.AddScoped<DataSeederService>();

#if DEBUG
            // Print registration info in debug mode
            services.PrintRegisteredAppServices();
#endif

            return services;
        }

    }
} 
