using NextAdmin.Application.Interfaces;

namespace NextAdmin.API.Extensions
{
    /// <summary>
    /// 服务集合扩展类
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加RGV实时通信服务
        /// </summary>
        public static IServiceCollection AddRgvRealtimeCommunication(this IServiceCollection services, IConfiguration configuration)
        {
            // 配置SignalR服务
            services.AddSignalR(options =>
            {
                // 从配置文件读取设置
                var signalRConfig = configuration.GetSection("SignalR:HubOptions");
                
                // 设置超时
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(
                    signalRConfig.GetValue<int>("ClientTimeoutInterval", 30));
                
                options.KeepAliveInterval = TimeSpan.FromSeconds(
                    signalRConfig.GetValue<int>("KeepAliveInterval", 15));
                
                options.HandshakeTimeout = TimeSpan.FromSeconds(
                    signalRConfig.GetValue<int>("HandshakeTimeout", 15));
                
                // 设置最大并行调用数
                options.MaximumParallelInvocationsPerClient = 
                    signalRConfig.GetValue<int>("MaximumParallelInvocationsPerClient", 1);
                
                // 启用详细错误信息
                options.EnableDetailedErrors = 
                    signalRConfig.GetValue<bool>("EnableDetailedErrors", false);
            });

            return services;
        }


        public static IServiceCollection AddGenerController(this IServiceCollection services)
        {
            services.AddControllers()
                .ConfigureApplicationPartManager(m =>
                {
                    m.FeatureProviders.Add(new GenericControllerFeatureProvider());
                })
                .AddJsonOptions(options =>
                {
                    // 统一 DateTime 输出格式
                    options.JsonSerializerOptions.Converters.Add(new NextAdmin.API.Extensions.Json.DateTimeConverter("yyyy-MM-dd HH:mm:ss"));
                    options.JsonSerializerOptions.Converters.Add(new NextAdmin.API.Extensions.Json.NullableDateTimeConverter("yyyy-MM-dd HH:mm:ss"));
                });

            // 注册
            services.AddControllers(options =>
            {
                options.Conventions.Add(new GenericControllerRouteConvention());
                options.Conventions.Add(new DynamicAuthorizeConvention());
            });
            return services;
        }
        public static IServiceCollection AddBaseController(this IServiceCollection services)
        {
            // 注册 FeatureProvider
            services.AddControllers()
                .PartManager.FeatureProviders
                .Add(new ControllerFeatureProvider());

            // 添加服务
            services.AddControllers(options =>
            {
                // 添加动态路由约定
                options.Conventions.Add(new ControllerRouteConvention());
                // 添加动态授权约定
                options.Conventions.Add(new ActionAuthorizeConvention());

            }).ConfigureApplicationPartManager(manager =>
            {
                // **关键修复**: 将 Application 程序集手动添加为应用部件，以确保其内部的服务能被发现
                manager.ApplicationParts.Add(new Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart(typeof(IAppService<,,,,,>).Assembly));

                //// 添加动态控制器特性提供程序
                //manager.FeatureProviders.Add(new ControllerFeatureProvider());
            });
            return services;
        }

        /// <summary>
        /// 配置CORS策略
        /// </summary>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", builder =>
                {
                    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                    
                    builder.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()
                          .SetIsOriginAllowed(_ => true); // 在开发环境中允许所有来源，包括localhost不同端口
                });
            });

            return services;
        }

        public static IServiceCollection AddCoreServices(this IServiceCollection services,IConfiguration configuration)
        {

            return services;
        }
    }
} 
