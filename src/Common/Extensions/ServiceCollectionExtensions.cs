using NextAdmin.Common.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
// using NextAdmin.Common.Services; // Removed as the namespace doesn't exist

namespace NextAdmin.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCommonServices(this IServiceCollection services)
        {
            // 注册通用服务 - Temporarily commented out as implementations are missing/misplaced
            // services.AddScoped<ICacheService, CacheService>();
            // services.AddScoped<ILoggingService, LoggingService>();
            // services.AddScoped<IValidationService, ValidationService>();
            // services.AddScoped<IExceptionHandlingService, ExceptionHandlingService>();
            // services.AddScoped<IPerformanceMonitoringService, PerformanceMonitoringService>();

            return services;
        }
    }
} 
