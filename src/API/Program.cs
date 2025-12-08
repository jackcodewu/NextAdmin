using AspNetCore.Identity.Mongo;
using NextAdmin.API.Authorization;
using NextAdmin.API.Extensions;
using NextAdmin.Application.Constants;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.Extensions;
using NextAdmin.Application.Interfaces;
using NextAdmin.Application.Services;
using NextAdmin.Common.Helpers;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Infrastructure.Configuration;
using NextAdmin.Infrastructure.Extensions;
using NextAdmin.Log;
using NextAdmin.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using TextCopy;

namespace NextAdmin.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Ensure configuration files can be hot-reloaded for dynamic retrieval of TenantId and other settings
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            // Configure NLog
            builder.Services.ConfigLogService(builder.Configuration);

            // Initialize logging system
            LogHelper.Initialize(
                queueSize: 10000,                    // Log queue size
                flushInterval: 100,                  // Flush interval (milliseconds)
                enableConsoleOutput: true,           // Enable console output
                logFilePath: "logs"                  // Log file path
            );

            builder.Services
                    .AddControllers()
                    .AddJsonOptions(o =>
                    {
                        o.JsonSerializerOptions.Converters.Add(new BsonDocumentJsonConverter());
                    });


            builder.Services.AddGenerController();

            builder.Services.AddEndpointsApiExplorer();

            // JWT
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;     // Added this line
                options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;    // Added this line
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in production environment
                options.SaveToken = true;
                options.IncludeErrorDetails = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true, // Enable lifetime validation as a fallback for Redis validation
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
                    ClockSkew = TimeSpan.Zero
                };

                // Use built-in JWT authentication event handler, add Redis validation logic
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var environment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                        var redisService = context.HttpContext.RequestServices.GetRequiredService<IRedisService>();
                        try
                        {
                            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            if (string.IsNullOrEmpty(userId))
                            {
                                context.Response.Headers.Append("X-Token-Expired", "true");
                                context.Fail("TOKEN validation failed: Missing user ID");
                                return;
                            }

                            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                          
                            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                            if (string.IsNullOrEmpty(token))
                            {
                                context.Response.Headers.Append("X-Token-Expired", "true");
                                context.Fail("TOKEN validation failed: Missing TOKEN in request header");
                                return;
                            }

                            var cancellationToken = context.HttpContext.RequestAborted;

                            if (!TryExtractTokenId(context.SecurityToken, token, out var tokenId))
                            {
                                if (environment.IsDevelopment())
                                {
                                    Console.WriteLine($"Development environment: Unable to parse TOKEN identifier, allowing access: {userId}");
                                }
                                else
                                {
                                    context.Response.Headers.Append("X-Token-Expired", "true");
                                    context.Fail("TOKEN validation failed: Unable to parse TOKEN identifier");
                                }
                                return;
                            }

                            if (!string.IsNullOrEmpty(tokenId))
                            {
                                var revokedKey = $"auth:token:revoked:{tokenId}";
                                var revokedMarker = await redisService.GetStringAsync(revokedKey);
                                if (!string.IsNullOrEmpty(revokedMarker))
                                {
                                    context.Response.Headers.Append("X-Token-Expired", "true");
                                    context.Fail("TOKEN validation failed: TOKEN has been revoked");
                                    return;
                                }
                            }

                            var isValidInRedis = await authService.ValidateTokenFromRedisAsync(userId, token, cancellationToken);
                            if (!isValidInRedis)
                            {
                                if (!TryExtractExpiry(context.SecurityToken, token, out var expiresAt))
                                {
                                    if (environment.IsDevelopment())
                                    {
                                        Console.WriteLine($"Development environment: TOKEN missing in Redis and unable to parse expiry time, allowing access: {userId}");
                                        return;
                                    }

                                    context.Response.Headers.Append("X-Token-Expired", "true");
                                    context.Fail("TOKEN validation failed: Unable to parse TOKEN validity period");
                                    return;
                                }

                                if (expiresAt <= DateTime.UtcNow)
                                {
                                    context.Response.Headers.Append("X-Token-Expired", "true");
                                    context.Fail("TOKEN validation failed: TOKEN has expired");
                                    return;
                                }

                                var restored = await authService.StoreTokenInRedisAsync(userId, token, expiresAt, cancellationToken);
                                if (!restored)
                                {
                                    if (environment.IsDevelopment())
                                    {
                                        Console.WriteLine($"Development environment: Redis restore TOKEN failed, but JWT valid, allowing access: {userId}");
                                        return;
                                    }

                                    context.Response.Headers.Append("X-Token-Expired", "true");
                                    context.Fail("TOKEN validation failed: Unable to restore Redis state");
                                    return;
                                }

                                Console.WriteLine($"TOKEN missing in Redis, automatically restored: {userId}");
                            }
                            else
                            {
                                Console.WriteLine($"TOKEN validated successfully in Redis: {userId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error occurred during Redis TOKEN verification: {ex.Message}");

                            if (environment.IsDevelopment())
                            {
                                Console.WriteLine("Development environment: Redis validation exception, but allowing continued access");
                            }
                            else
                            {
                                context.Response.Headers.Append("X-Token-Expired", "true");
                                context.Fail("TOKEN validation failed: Error occurred during validation process");
                            }
                        }
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"JWT authentication failed: {context.Exception.Message}");
                        
                        // Add response header to prompt frontend to clear Token
                        context.Response.Headers.Append("X-Token-Expired", "true");
                        context.Response.Headers.Append("WWW-Authenticate", "Bearer error=\"invalid_token\", error_description=\"Token validation failed\"");
                        
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        // Customize 401 response, prompt user to log in again
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        context.Response.Headers.Append("X-Token-Expired", "true");
                        
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            success = false,
                            code = "401",
                            message = context.ErrorDescription ?? "Authentication failed, please log in again"
                        });
                        
                        return context.Response.WriteAsync(result);
                    },
                    OnMessageReceived = context =>
                    {
                        // Get TOKEN from query string (for SignalR and similar scenarios)
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/real-time-data"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Register HttpContextAccessor
            builder.Services.AddHttpContextAccessor();

            // Register HttpClient factory
            builder.Services.AddHttpClient();

            // Add distributed cache service (Redis)
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "NextAdmin_";
            });

            // Local memory cache, used by RedisService as L1 cache
            builder.Services.AddMemoryCache();

            // Add CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            // Add SignalR services
            //builder.Services.AddRealTimeSignalR(builder.Configuration);

            // Register infrastructure layer services
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // Register application layer services
            builder.Services.AddApplicationServices(builder.Configuration);

            builder.Services.AddOptions<JwtSettings>()
                .BindConfiguration(JwtSettings.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Read custom configuration
            var customSettings = builder.Configuration.GetSection(MongoDbSettings.SectionName).Get<MongoDbSettings>();
            if (customSettings is null)
            {
                throw new InvalidOperationException("MongoDB settings are missing in configuration.");
            }
            // Register MongoDB Identity
            builder.Services.AddIdentityMongoDbProvider<ApplicationUser, ApplicationRole, ObjectId>(
                identityOptions =>
                {
                    identityOptions.Password.RequireDigit = false;
                    identityOptions.Password.RequiredLength = 6;
                    identityOptions.Password.RequireNonAlphanumeric = false;
                    identityOptions.Password.RequireUppercase = false;
                    identityOptions.Password.RequireLowercase = false;
                    identityOptions.User.RequireUniqueEmail = true;
                    identityOptions.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                    identityOptions.SignIn.RequireConfirmedEmail = false;
                    identityOptions.SignIn.RequireConfirmedPhoneNumber = false;
                    identityOptions.Lockout.AllowedForNewUsers = true;
                    identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    identityOptions.Lockout.MaxFailedAccessAttempts = 5;
                },
                mongoOptions =>
                {
                    mongoOptions.ConnectionString = $"{customSettings.ConnectionString}";
                });

            // Disable Identity's Cookie authentication
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "Disabled";
                options.ExpireTimeSpan = TimeSpan.FromSeconds(1);
                options.SlidingExpiration = false;
                options.LoginPath = null;           // Added
                options.LogoutPath = null;          // Added  
                options.AccessDeniedPath = null;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };
            });

            // Authorization configuration
            builder.Services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();
            builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            builder.Services.AddAuthorization(options =>
            {
                // Automatically register all permission policies from PermissionsDefine class
                var permissionType = typeof(PermissionsDefine);
                var permissionValues = new HashSet<string>(StringComparer.Ordinal);

                static void CollectPermissions(IEnumerable<FieldInfo> fieldsToScan, ISet<string> destination)
                {
                    foreach (var field in fieldsToScan)
                    {
                        if (!field.IsLiteral || field.IsInitOnly)
                        {
                            continue;
                        }

                        if (field.GetValue(null) is string value && !string.IsNullOrWhiteSpace(value))
                        {
                            destination.Add(value);
                        }
                    }
                }

                var fields = permissionType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                CollectPermissions(fields, permissionValues);

                var nestedTypes = permissionType.GetNestedTypes(BindingFlags.Public);
                foreach (var type in nestedTypes)
                {
                    var nestedFields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    CollectPermissions(nestedFields, permissionValues);
                }

                foreach (var permission in permissionValues)
                {
                    options.AddPolicy(permission, policy =>
                        policy.Requirements.Add(new PermissionRequirement(permission)));
                }

                // Retain other manual policies you may need
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
            });

            // Register application services
            builder.Services.AddScoped<IAuthService, AuthService>();

            // Problem details configuration
            builder.Services.AddProblemDetails();

            // Swagger configuration
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "NextAdmin API",
                    Version = "v1",
                    Description = "General backend management system"
                });
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "NextAdmin.API.xml"), true);

                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "NextAdmin.Application.xml"), true);

                // Make Swagger see BsonDocument as a regular object (additionalProperties arbitrary)
                c.MapType<BsonDocument>(() => new OpenApiSchema
                {
                    Type = "object",
                    AdditionalPropertiesAllowed = true
                });

                // JWT authentication configuration
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Include XML comments
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            // Health checks
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            // Enable serving static files (required for custom Swagger assets)
            app.UseStaticFiles();

            // Configure HTTP request pipeline

            // Swagger enabled in all environments (including IIS production environment)
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "NextAdmin API v1");
                c.DocumentTitle = "NextAdmin API Documentation";
                
                // Enable authorization information persistence (retain Token after page refresh)
                c.ConfigObject.PersistAuthorization = true;
                c.EnablePersistAuthorization();

                // Custom script for additional persistence of Swagger authorization state
                c.InjectJavascript("/swagger/swagger-auth-persist.js");
            });

            // Use HTTPS redirect only in production environment
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            // Add exception handling middleware
            app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();

            // Add WebSocket support
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            });

            // SignalR Hub mapping removed, changed to frontend polling API at intervals

            app.MapControllers();

            //// Map SignalR Hub
            //app.MapHub<RealTimeDataHub>("/api/real-time-data");

            // Execute database migrations
            await ExecuteDatabaseMigrationsAsync(app);

            bool needSeed = !builder.Configuration.GetValue<bool>("SeedData");
            if (needSeed)
            {
                // Initialize default data
                await SeedDatabaseAsync(app);
                ConfigHelper.UpdateSeedData(true);
            }

            if (app.Environment.IsDevelopment())
            {
                await AutoLoginAsync(app);
            }

            app.Run();
        }
        private static bool TryExtractExpiry(SecurityToken securityToken, string rawToken, out DateTime expiresAt)
        {
            if (securityToken is JwtSecurityToken jwtToken)
            {
                expiresAt = jwtToken.ValidTo;
                return true;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var parsedToken = handler.ReadJwtToken(rawToken);
                expiresAt = parsedToken.ValidTo;
                return true;
            }
            catch
            {
                expiresAt = DateTime.MinValue;
                return false;
            }
        }

        private static bool TryExtractTokenId(SecurityToken securityToken, string rawToken, out string tokenId)
        {
            if (securityToken is JwtSecurityToken jwtToken)
            {
                tokenId = jwtToken.Id ?? string.Empty;
                return true;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var parsedToken = handler.ReadJwtToken(rawToken);
                tokenId = parsedToken.Id ?? string.Empty;
                return true;
            }
            catch
            {
                tokenId = string.Empty;
                return false;
            }
        }

        static async Task AutoLoginAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var authService = services.GetRequiredService<IAuthService>();
                var loginRequest = new AuthDtos.LoginRequest(
                    "admin",           // UserName
                    "Admin123!",       // Password
                    "dev",             // CaptchaToken
                    0,                 // CaptchaX
                    "dev",             // CaptchaTrack
                    true,
                    true
                    );

                var result = await authService.LoginAsync(loginRequest);
                if (result.Success)
                {
                    ClipboardService.SetText($"Bearer {result.Token}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("An error occurred during data seeding.", ex);
            }
        }

        static async Task ExecuteDatabaseMigrationsAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var migrationService = services.GetRequiredService<DatabaseMigrationService>();
                var success = await migrationService.ExecuteMigrationsAsync();
                if (success)
                {
                    LogHelper.Info("Database migration execution completed");
                }
                else
                {
                    LogHelper.Error("Database migration execution failed");
                }

                // Initialize system data
                var seederService = services.GetRequiredService<DataSeederService>();
                await seederService.SeedAsync();
            }
            catch (Exception ex)
            {
                LogHelper.Error("Error occurred during database migration", ex);
            }
        }

        static async Task SeedDatabaseAsync(WebApplication app)
        {
            //using var scope = app.Services.CreateScope();
            //var services = scope.ServiceProvider;
            //try
            //{
            //    var dataSeeder = services.GetRequiredService<IDataSeeder>();
            //    await dataSeeder.SeedAsync();
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Error("An error occurred during data seeding.", ex);
            //}
        }
    }
}
