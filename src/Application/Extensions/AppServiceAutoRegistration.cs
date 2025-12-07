using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoMapper;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.Interfaces;
using NextAdmin.Application.Services;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace NextAdmin.Application.Extensions
{
    /// <summary>
    /// 应用服务自动注册扩展
    /// 自动为继承 AggregateRoot 的实体生成并注册应用服务
    /// 支持生成分部类以扩展已存在的自定义服务
    /// </summary>
    public static class AppServiceAutoRegistration
    {
        /// <summary>
        /// 自动注册所有应用服务
        /// 扫描所有继承 AggregateRoot 的实体，自动注册对应的 AppService
        /// 如果自定义服务已存在，则生成分部类文件
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="generatePartialClasses">是否生成分部类文件（默认：true）</param>
        /// <param name="outputDirectory">分部类输出目录（默认：Application/Services/Generated）</param>
        /// <param name="assemblies">要扫描的程序集</param>
        public static IServiceCollection AddAutoAppServices(
            this IServiceCollection services,
            bool generatePartialClasses = true,
            string? outputDirectory = null,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                assemblies = new[]
                {
                    Assembly.Load("NextAdmin.Core"),
                    Assembly.Load("NextAdmin.Application")
                };
            }

            // 设置默认输出目录
            if (string.IsNullOrEmpty(outputDirectory))
            {
                // 获取 Application 项目的根目录
                var appAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "NextAdmin.Application");
                if (appAssembly != null)
                {
                    var codeBase = appAssembly.Location;
                    var projectRoot = Directory.GetParent(codeBase)?.Parent?.Parent?.Parent?.FullName;
                    outputDirectory = projectRoot != null 
                        ? Path.Combine(projectRoot, "Services", "Generated")
                        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");
                }
                else
                {
                    outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");
                }
            }

            // 确保输出目录存在
            if (generatePartialClasses && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            Console.WriteLine("=== 应用服务自动注册 ===");
            if (generatePartialClasses)
            {
                Console.WriteLine($"📁 分部类输出目录: {outputDirectory}");
            }

            // 排除的实体类型（Identity 相关等特殊实体）
            var excludedTypes = new HashSet<string>
            {
                "ApplicationUser",
                "ApplicationRole"
            };

            // 查找所有继承 AggregateRoot 的实体类
            var entityTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    !type.IsGenericTypeDefinition &&
                    typeof(AggregateRoot).IsAssignableFrom(type) &&
                    !excludedTypes.Contains(type.Name)) // 排除特殊实体
                .ToList();

            Console.WriteLine($"📋 发现 {entityTypes.Count} 个实体类（已排除 Identity 相关实体）");

            foreach (var entityType in entityTypes)
            {
                RegisterAppServiceForEntity(services, entityType, assemblies, generatePartialClasses, outputDirectory);
            }

            Console.WriteLine("✅ 应用服务自动注册完成");
            Console.WriteLine();

            return services;
        }

        /// <summary>
        /// 为单个实体注册应用服务
        /// </summary>
        private static void RegisterAppServiceForEntity(
            IServiceCollection services,
            Type entityType,
            Assembly[] assemblies,
            bool generatePartialClasses,
            string outputDirectory)
        {
            var entityName = entityType.Name;

            // 查找对应的 DTO 类型
            var dtoTypes = FindDtoTypes(entityName, assemblies);
            if (dtoTypes == null)
            {
                Console.WriteLine($"⚠️  {entityName}: 未找到完整的 DTO 类型，跳过");
                return;
            }

            // 查找自定义应用服务接口和实现
            var serviceInterfaceName = $"I{entityName}Service";
            var serviceImplementationName = $"{entityName}Service";

            var serviceInterface = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type =>
                    type.IsInterface &&
                    type.Name == serviceInterfaceName);

            var serviceImplementation = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    type.Name == serviceImplementationName);

            // 情况 1：接口和实现都存在（生成分部类）
            if (serviceInterface != null && serviceImplementation != null)
            {
                if (serviceInterface.IsAssignableFrom(serviceImplementation))
                {
                    services.AddScoped(serviceInterface, serviceImplementation);
                    
                    // 生成分部类文件
                    if (generatePartialClasses)
                    {
                        // 查找自定义仓储接口
                        var repositoryInterfaceName = $"I{entityName}Repository";
                        var customRepositoryInterface = assemblies
                            .SelectMany(assembly => assembly.GetTypes())
                            .FirstOrDefault(type => type.IsInterface && type.Name == repositoryInterfaceName);
                        
                        GeneratePartialServiceClass(
                            entityType, 
                            entityName, 
                            serviceImplementationName, 
                            dtoTypes, 
                            outputDirectory,
                            customRepositoryInterface);
                        Console.WriteLine($"✅ {entityName}: 已注册自定义服务并生成分部类 {serviceInterfaceName} -> {serviceImplementationName}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ {entityName}: 已注册自定义服务 {serviceInterfaceName} -> {serviceImplementationName}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️  {entityName}: {serviceImplementationName} 未实现 {serviceInterfaceName}");
                }
                return;
            }

            // 情况 2：只有接口，没有实现（生成默认实现）
            if (serviceInterface != null && serviceImplementation == null)
            {
                RegisterGenericAppService(services, entityType, dtoTypes, serviceInterface);
                Console.WriteLine($"🔧 {entityName}: 使用泛型服务实现 {serviceInterfaceName}");
                return;
            }

            // 情况 3：没有自定义接口和实现（使用默认泛型服务）
            RegisterGenericAppService(services, entityType, dtoTypes, null);
            Console.WriteLine($"📦 {entityName}: 注册泛型服务 IAppService<{entityName}, ...>");
        }

        /// <summary>
        /// 查找实体对应的 DTO 类型
        /// </summary>
        private static DtoTypes? FindDtoTypes(string entityName, Assembly[] assemblies)
        {
            var baseDtoName = $"{entityName}Dto";
            var createDtoName = $"Create{entityName}Dto";
            var updateDtoName = $"Update{entityName}Dto";
            var queryDtoName = $"{entityName}QueryDto";
            var basesDtoName = $"{entityName}sDto";

            var allTypes = assemblies.SelectMany(a => a.GetTypes()).ToList();

            var baseDto = allTypes.FirstOrDefault(t => t.Name == baseDtoName && typeof(BaseDto).IsAssignableFrom(t));
            var createDto = allTypes.FirstOrDefault(t => t.Name == createDtoName && typeof(CreateDto).IsAssignableFrom(t));
            var updateDto = allTypes.FirstOrDefault(t => t.Name == updateDtoName && typeof(UpdateDto).IsAssignableFrom(t));
            var queryDto = allTypes.FirstOrDefault(t => t.Name == queryDtoName && t.IsClass);
            var basesDto = allTypes.FirstOrDefault(t => t.Name == basesDtoName && typeof(BasesDto).IsAssignableFrom(t));

            // 至少需要 BaseDto 和 CreateDto
            if (baseDto == null || createDto == null)
            {
                return null;
            }

            // 使用默认类型填充缺失的 DTO
            return new DtoTypes
            {
                BaseDto = baseDto,
                CreateDto = createDto,
                UpdateDto = updateDto ?? createDto, // 如果没有 UpdateDto，使用 CreateDto
                QueryDto = queryDto ?? typeof(QueryDto<>).MakeGenericType(baseDto.BaseType?.GetGenericArguments()[0] ?? typeof(object)),
                BasesDto = basesDto ?? baseDto // 如果没有 BasesDto，使用 BaseDto
            };
        }

        /// <summary>
        /// 注册泛型应用服务
        /// </summary>
        private static void RegisterGenericAppService(
            IServiceCollection services,
            Type entityType,
            DtoTypes dtoTypes,
            Type? customInterface)
        {
            // 构建泛型服务类型
            var serviceType = typeof(AppService<,,,,,>).MakeGenericType(
                entityType,
                dtoTypes.BaseDto,
                dtoTypes.CreateDto,
                dtoTypes.UpdateDto,
                dtoTypes.QueryDto,
                dtoTypes.BasesDto);

            // 构建接口类型
            var interfaceType = customInterface ?? typeof(IAppService<,,,,,>).MakeGenericType(
                entityType,
                dtoTypes.BaseDto,
                dtoTypes.CreateDto,
                dtoTypes.UpdateDto,
                dtoTypes.QueryDto,
                dtoTypes.BasesDto);

            // 注册服务
            services.AddScoped(interfaceType, sp =>
            {
                var repository = sp.GetRequiredService(
                    typeof(IBaseRepository<>).MakeGenericType(entityType));
                var mapper = sp.GetRequiredService<IMapper>();
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

                return Activator.CreateInstance(
                    serviceType,
                    repository,
                    mapper,
                    httpContextAccessor,
                    null,  // key
                    false, // isCommanyId
                    true   // isCache
                )!;
            });
        }

        /// <summary>
        /// 生成分部类服务文件
        /// </summary>
        private static void GeneratePartialServiceClass(
            Type entityType,
            string entityName,
            string serviceClassName,
            DtoTypes dtoTypes,
            string outputDirectory,
            Type? customRepositoryInterface)
        {
            var fileName = $"{serviceClassName}.Generated.cs";
            var filePath = Path.Combine(outputDirectory, fileName);

            // 获取自定义仓储的命名空间和名称
            string? customRepositoryNamespace = customRepositoryInterface?.Namespace;
            string? customRepositoryName = customRepositoryInterface?.Name;
            bool hasCustomRepository = customRepositoryInterface != null;

            // 构建分部类代码
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("// 此文件由 AppServiceAutoRegistration 自动生成");
            sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq.Expressions;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using AutoMapper;");
            sb.AppendLine("using NextAdmin.Application.Services;");
            sb.AppendLine("using NextAdmin.Core.Domain.Entities;");
            sb.AppendLine("using NextAdmin.Core.Domain.Interfaces.Repositories;");
            sb.AppendLine("using Microsoft.AspNetCore.Http;");
            sb.AppendLine("using MongoDB.Bson;");
            
            // 如果有自定义仓储，添加其命名空间
            if (hasCustomRepository && !string.IsNullOrEmpty(customRepositoryNamespace))
            {
                sb.AppendLine($"using {customRepositoryNamespace};");
            }
            
            sb.AppendLine();
            sb.AppendLine("namespace NextAdmin.Application.Services");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {entityName} 服务 - 自动生成的分部类");
            sb.AppendLine($"    /// 包含基础 CRUD 操作");
            if (hasCustomRepository)
            {
                sb.AppendLine($"    /// 使用自定义仓储: {customRepositoryName}");
            }
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public partial class {serviceClassName}");
            sb.AppendLine("    {");
            
            // 添加自定义仓储字段引用
            if (hasCustomRepository)
            {
                sb.AppendLine($"        // 自定义仓储引用（通过构造函数注入）");
                sb.AppendLine($"        // private readonly {customRepositoryName} _customRepository;");
                sb.AppendLine($"        // 可以通过以下方式获取自定义仓储:");
                sb.AppendLine($"        // var customRepo = _baseRepository as {customRepositoryName};");
                sb.AppendLine($"        // 或在构造函数中注入: {customRepositoryName} customRepository");
                sb.AppendLine();
            }
            
            sb.AppendLine("        // 此分部类由系统自动生成，包含基础 CRUD 功能");
            sb.AppendLine("        // 可以在另一个分部类文件中添加自定义业务逻辑");
            sb.AppendLine();
            sb.AppendLine("        #region 自动生成的基础方法");
            sb.AppendLine();
            
            // 生成基础 CRUD 方法的提示注释
            sb.AppendLine("        // 基础 CRUD 方法已由 AppService 基类提供：");
            sb.AppendLine("        // - Task<TBaseDto?> AddAsync(TCreateDto dto)");
            sb.AppendLine("        // - Task<TBaseDto?> UpdateAsync(TUpdateDto dto)");
            sb.AppendLine("        // - Task<bool> DeleteAsync(string id)");
            sb.AppendLine("        // - Task<TBaseDto?> GetAsync(string id)");
            sb.AppendLine("        // - Task<List<TBasesDto>> GetsAsync(Expression<Func<TEntity, bool>>? filter = null)");
            sb.AppendLine("        // - Task<QueryPageResultDto<TBasesDto>> GetListPageAsync(TQueryDto queryDto)");
            sb.AppendLine();
            
            // 添加一些辅助方法示例
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 检查 {entityName} 是否存在");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public async Task<bool> ExistsAsync(string id)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (string.IsNullOrEmpty(id)) return false;");
            sb.AppendLine("            var entity = await _baseRepository.GetAsync(e => e.Id == id);");
            sb.AppendLine("            return entity != null;");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 批量获取 {entityName}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public async Task<List<TBasesDto>> GetByIdsAsync(IEnumerable<string> ids)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (ids == null || !ids.Any()) return new List<TBasesDto>();");
            sb.AppendLine("            var entities = await _baseRepository.GetsAsync(e => ids.Contains(e.Id));");
            sb.AppendLine("            return Mapper.Map<List<TBasesDto>>(entities);");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 获取已启用的 {entityName} 列表");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public async Task<List<TBasesDto>> GetEnabledAsync()");
            sb.AppendLine("        {");
            sb.AppendLine("            return await GetsAsync(e => e.IsEnabled);");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 批量启用/禁用 {entityName}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public async Task<bool> SetEnabledAsync(IEnumerable<string> ids, bool enabled)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (ids == null || !ids.Any()) return false;");
            sb.AppendLine("            foreach (var id in ids)");
            sb.AppendLine("            {");
            sb.AppendLine("                var entity = await _baseRepository.GetAsync(e => e.Id == id);");
            sb.AppendLine("                if (entity != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    entity.IsEnabled = enabled;");
            sb.AppendLine("                    await _baseRepository.UpdateAsync(entity);");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 如果有自定义仓储，添加获取自定义仓储的辅助方法
            if (hasCustomRepository)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// 获取自定义仓储 {customRepositoryName}");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        protected {customRepositoryName}? GetCustomRepository()");
                sb.AppendLine("        {");
                sb.AppendLine($"            return _baseRepository as {customRepositoryName};");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// 获取自定义仓储 {customRepositoryName}（强制转换，如果失败会抛出异常）");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        protected {customRepositoryName} GetCustomRepositoryOrThrow()");
                sb.AppendLine("        {");
                sb.AppendLine($"            return (_baseRepository as {customRepositoryName}) ?? ");
                sb.AppendLine($"                throw new InvalidOperationException(\"无法将 _baseRepository 转换为 {customRepositoryName}\");");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
            
            sb.AppendLine("        #endregion");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            // 写入文件
            try
            {
                File.WriteAllText(filePath, sb.ToString());
                Console.WriteLine($"   📄 已生成分部类: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  生成分部类失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 打印已注册的应用服务列表（用于调试）
        /// </summary>
        public static void PrintRegisteredAppServices(this IServiceCollection services)
        {
            Console.WriteLine();
            Console.WriteLine("=== 已注册的应用服务列表 ===");

            var appServices = services
                .Where(sd => sd.ServiceType.IsGenericType &&
                            (sd.ServiceType.GetGenericTypeDefinition() == typeof(IAppService<,,,,,>) ||
                             sd.ServiceType.Name.EndsWith("Service")))
                .ToList();

            foreach (var service in appServices)
            {
                var serviceName = service.ServiceType.Name;
                var implementationName = service.ImplementationType?.Name ?? 
                                        service.ImplementationFactory?.Method.ReturnType.Name ?? 
                                        "Factory";

                Console.WriteLine($"  {serviceName} -> {implementationName}");
            }

            Console.WriteLine($"📊 共注册 {appServices.Count} 个应用服务");
            Console.WriteLine();
        }

        /// <summary>
        /// DTO 类型集合
        /// </summary>
        private class DtoTypes
        {
            public Type BaseDto { get; set; } = null!;
            public Type CreateDto { get; set; } = null!;
            public Type UpdateDto { get; set; } = null!;
            public Type QueryDto { get; set; } = null!;
            public Type BasesDto { get; set; } = null!;
        }
    }
}
