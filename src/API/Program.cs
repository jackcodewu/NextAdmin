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
            // 确保配置文件变更可热加载，便于动态获取TenantId等配置
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            // 配置 NLog
            builder.Services.ConfigLogService(builder.Configuration);

            // 初始化日志系统
            LogHelper.Initialize(
                queueSize: 10000,                    // 日志队列大小
                flushInterval: 100,                  // 刷新间隔(毫秒)
                enableConsoleOutput: true,           // 启用控制台输出
                logFilePath: "logs"                  // 日志文件路径
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
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;     // 新增这行
                options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;    // 新增这行
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // 在生产环境中设置为 true
                options.SaveToken = true;
                options.IncludeErrorDetails = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true, // 启用生命周期验证，作为Redis验证的备用方案
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
                    ClockSkew = TimeSpan.Zero
                };

                // 使用系统内置的JWT认证事件处理器，添加Redis验证逻辑
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
                                context.Fail("TOKEN验证失败：缺少用户ID");
                                return;
                            }

                            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                          
                            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                            if (string.IsNullOrEmpty(token))
                            {
                                context.Response.Headers.Append("X-Token-Expired", "true");
                                context.Fail("TOKEN验证失败：请求头中缺少TOKEN");
                                return;
                            }

                            var cancellationToken = context.HttpContext.RequestAborted;

                            if (!TryExtractTokenId(context.SecurityToken, token, out var tokenId))
                            {
                                if (environment.IsDevelopment())
                                {
                                    Console.WriteLine($"开发环境：无法解析TOKEN标识，允许访问：{userId}");
                                }
                                else
                                {
                                    context.Response.Headers.Append("X-Token-Expired", "true");
                                    context.Fail("TOKEN验证失败：无法解析TOKEN标识");
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
                                    context.Fail("TOKEN验证失败：TOKEN已被注销");
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
                                        Console.WriteLine($"开发环境：TOKEN在Redis中缺失且无法解析过期时间，允许访问：{userId}");
                                        return;
                                    }

                                    context.Response.Headers.Append("X-Token-Expired", "true");
                                    context.Fail("TOKEN验证失败：无法解析TOKEN有效期");
                                    return;
                                }

                                if (expiresAt <= DateTime.UtcNow)
                                {
                                    context.Response.Headers.Append("X-Token-Expired", "true");
                                    context.Fail("TOKEN验证失败：TOKEN已过期");
                                    return;
                                }

                                var restored = await authService.StoreTokenInRedisAsync(userId, token, expiresAt, cancellationToken);
                                if (!restored)
                                {
                                    if (environment.IsDevelopment())
                                    {
                                        Console.WriteLine($"开发环境：Redis恢复TOKEN失败，但JWT有效，允许访问：{userId}");
                                        return;
                                    }

                                    context.Response.Headers.Append("X-Token-Expired", "true");
                                    context.Fail("TOKEN验证失败：无法恢复Redis状态");
                                    return;
                                }

                                Console.WriteLine($"TOKEN在Redis中缺失，已自动恢复：{userId}");
                            }
                            else
                            {
                                Console.WriteLine($"TOKEN在Redis中验证成功：{userId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Redis TOKEN验证过程中发生错误: {ex.Message}");

                            if (environment.IsDevelopment())
                            {
                                Console.WriteLine("开发环境：Redis验证异常，但允许继续访问");
                            }
                            else
                            {
                                context.Response.Headers.Append("X-Token-Expired", "true");
                                context.Fail("TOKEN验证失败：验证过程中发生错误");
                            }
                        }
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"JWT认证失败: {context.Exception.Message}");
                        
                        // 添加响应头，提示前端清除 Token
                        context.Response.Headers.Append("X-Token-Expired", "true");
                        context.Response.Headers.Append("WWW-Authenticate", "Bearer error=\"invalid_token\", error_description=\"Token validation failed\"");
                        
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        // 自定义 401 响应，提示用户重新登录
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        context.Response.Headers.Append("X-Token-Expired", "true");
                        
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            success = false,
                            code = "401",
                            message = context.ErrorDescription ?? "身份验证失败，请重新登录"
                        });
                        
                        return context.Response.WriteAsync(result);
                    },
                    OnMessageReceived = context =>
                    {
                        // 从查询字符串获取TOKEN（用于SignalR等场景）
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

            // 注册 HttpContextAccessor
            builder.Services.AddHttpContextAccessor();

            // 注册 HttpClient 工厂
            builder.Services.AddHttpClient();

            // 添加分布式缓存服务（Redis）
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "NextAdmin_";
            });

            // 本地内存缓存，用于 RedisService 作为一级缓存
            builder.Services.AddMemoryCache();

            // 添加CORS策略
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

            // 添加SignalR服务
            //builder.Services.AddRealTimeSignalR(builder.Configuration);

            // 注册基础设施层服务
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // 注册应用层服务
            builder.Services.AddApplicationServices(builder.Configuration);

            builder.Services.AddOptions<JwtSettings>()
                .BindConfiguration(JwtSettings.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // 读取自定义配置
            var customSettings = builder.Configuration.GetSection(MongoDbSettings.SectionName).Get<MongoDbSettings>();
            if (customSettings is null)
            {
                throw new InvalidOperationException("MongoDB settings are missing in configuration.");
            }
            // 注册 MongoDB Identity
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

            // 禁用 Identity 的 Cookie 认证
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "Disabled";
                options.ExpireTimeSpan = TimeSpan.FromSeconds(1);
                options.SlidingExpiration = false;
                options.LoginPath = null;           // 新增
                options.LogoutPath = null;          // 新增  
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

            // 授权配置
            builder.Services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();
            builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            builder.Services.AddAuthorization(options =>
            {
                // 自动从PermissionsDefine类中注册所有权限策略
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

                // 保留您可能需要的其他手动策略
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
            });

            // 注册应用服务
            builder.Services.AddScoped<IAuthService, AuthService>();

            // 问题详细信息配置
            builder.Services.AddProblemDetails();

            // Swagger 配置
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "NextAdmin API",
                    Version = "v1",
                    Description = "通用后台管理系统"
                });
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "NextAdmin.API.xml"), true);

                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "NextAdmin.Application.xml"), true);

                // 让 Swagger 把 BsonDocument 看成一个普通对象（additionalProperties 任意）
                c.MapType<BsonDocument>(() => new OpenApiSchema
                {
                    Type = "object",
                    AdditionalPropertiesAllowed = true
                });

                // JWT 认证配置
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

                // 包含XML注释
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            // 健康检查
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            // Enable serving static files (required for custom Swagger assets)
            app.UseStaticFiles();

            // 配置HTTP请求管道

            // Swagger 在所有环境启用（包括 IIS 生产环境）
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "NextAdmin API v1");
                c.DocumentTitle = "NextAdmin API 文档";
                
                // 启用授权信息持久化（刷新页面后保留 Token）
                c.ConfigObject.PersistAuthorization = true;
                c.EnablePersistAuthorization();

                // 自定义脚本，用于额外持久化 Swagger 授权状态
                c.InjectJavascript("/swagger/swagger-auth-persist.js");
            });

            // 仅在生产环境中使用HTTPS重定向
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            // 添加异常处理中间件
            app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();

            // 添加WebSocket支持
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            });

            // SignalR Hub映射已移除，改为前端定时轮询API

            app.MapControllers();

            //// 映射SignalR Hub
            //app.MapHub<RealTimeDataHub>("/api/real-time-data");

            // 执行数据库迁移
            await ExecuteDatabaseMigrationsAsync(app);

            bool needSeed = !builder.Configuration.GetValue<bool>("SeedData");
            if (needSeed)
            {
                // 初始化默认数据
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
                    LogHelper.Info("数据库迁移执行完成");
                }
                else
                {
                    LogHelper.Error("数据库迁移执行失败");
                }

                // 初始化系统数据
                var seederService = services.GetRequiredService<DataSeederService>();
                await seederService.SeedAsync();
            }
            catch (Exception ex)
            {
                LogHelper.Error("执行数据库迁移时发生错误", ex);
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
