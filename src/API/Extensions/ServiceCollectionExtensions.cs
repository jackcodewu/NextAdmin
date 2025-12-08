using NextAdmin.Application.Interfaces;

namespace NextAdmin.API.Extensions
{
    /// <summary>
    /// Service collection extension class
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add RGV real-time communication service
        /// </summary>
        public static IServiceCollection AddRgvRealtimeCommunication(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure SignalR service
            services.AddSignalR(options =>
            {
                // Read settings from configuration file
                var signalRConfig = configuration.GetSection("SignalR:HubOptions");
                
                // Set timeouts
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(
                    signalRConfig.GetValue<int>("ClientTimeoutInterval", 30));
                
                options.KeepAliveInterval = TimeSpan.FromSeconds(
                    signalRConfig.GetValue<int>("KeepAliveInterval", 15));
                
                options.HandshakeTimeout = TimeSpan.FromSeconds(
                    signalRConfig.GetValue<int>("HandshakeTimeout", 15));
                
                // Set maximum parallel calls
                options.MaximumParallelInvocationsPerClient = 
                    signalRConfig.GetValue<int>("MaximumParallelInvocationsPerClient", 1);
                
                // Enable detailed error information
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
                    // Unified DateTime output format
                    options.JsonSerializerOptions.Converters.Add(new NextAdmin.API.Extensions.Json.DateTimeConverter("yyyy-MM-dd HH:mm:ss"));
                    options.JsonSerializerOptions.Converters.Add(new NextAdmin.API.Extensions.Json.NullableDateTimeConverter("yyyy-MM-dd HH:mm:ss"));
                });

            // Register
            services.AddControllers(options =>
            {
                options.Conventions.Add(new GenericControllerRouteConvention());
                options.Conventions.Add(new DynamicAuthorizeConvention());
            });
            return services;
        }
        public static IServiceCollection AddBaseController(this IServiceCollection services)
        {
            // Register FeatureProvider
            services.AddControllers()
                .PartManager.FeatureProviders
                .Add(new ControllerFeatureProvider());

            // Add services
            services.AddControllers(options =>
            {
                // Add dynamic routing convention
                options.Conventions.Add(new ControllerRouteConvention());
                // Add dynamic authorization convention
                options.Conventions.Add(new ActionAuthorizeConvention());

            }).ConfigureApplicationPartManager(manager =>
            {
                // **Critical fix**: Manually add Application assembly as application part to ensure its internal services can be discovered
                manager.ApplicationParts.Add(new Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart(typeof(IAppService<,,,,,>).Assembly));

                //// Add dynamic controller feature provider
                //manager.FeatureProviders.Add(new ControllerFeatureProvider());
            });
            return services;
        }

        /// <summary>
        /// Configure CORS policy
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
                          .SetIsOriginAllowed(_ => true); // Allow all origins in development environment, including localhost different ports
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
