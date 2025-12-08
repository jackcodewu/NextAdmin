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
    /// Application service auto-registration extension
    /// Automatically generates and registers application services for entities inheriting AggregateRoot
    /// Supports generating partial classes to extend existing custom services
    /// </summary>
    public static class AppServiceAutoRegistration
    {
        /// <summary>
        /// Auto-register all application services
        /// Scan all entities inheriting AggregateRoot and automatically register corresponding AppService
        /// If custom service already exists, generate partial class file
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="generatePartialClasses">Whether to generate partial class files (default: true)</param>
        /// <param name="outputDirectory">Partial class output directory (default: Application/Services/Generated)</param>
        /// <param name="assemblies">Assemblies to scan</param>
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

            // Set default output directoryult output directory
            if (string.IsNullOrEmpty(outputDirectory))
            {
                // Get Application project root directory
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

            // Ensure output directory exists
            if (generatePartialClasses && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            Console.WriteLine("=== Application Service Auto-Registration ===");
            if (generatePartialClasses)
            {
                Console.WriteLine($"📁 Partial class output directory: {outputDirectory}");
            }

            // Excluded entity types (Identity-related and other special entities)
            var excludedTypes = new HashSet<string>
            {
                "ApplicationUser",
                "ApplicationRole"
            };

            // Find all entity classes inheriting AggregateRoot
            var entityTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    !type.IsGenericTypeDefinition &&
                    typeof(AggregateRoot).IsAssignableFrom(type) &&
                    !excludedTypes.Contains(type.Name)) // Exclude special entities
                .ToList();

            Console.WriteLine($"📋 Found {entityTypes.Count} entity classes (Identity-related entities excluded)");

            foreach (var entityType in entityTypes)
            {
                RegisterAppServiceForEntity(services, entityType, assemblies, generatePartialClasses, outputDirectory);
            }

            Console.WriteLine("✅ Application service auto-registration completed");
            Console.WriteLine();

            return services;
        }

        /// <summary>
        /// Register application service for a single entity
        /// </summary>
        private static void RegisterAppServiceForEntity(
            IServiceCollection services,
            Type entityType,
            Assembly[] assemblies,
            bool generatePartialClasses,
            string outputDirectory)
        {
            var entityName = entityType.Name;

            // Find corresponding DTO types
            var dtoTypes = FindDtoTypes(entityName, assemblies);
            if (dtoTypes == null)
            {
                Console.WriteLine($"⚠️  {entityName}: Complete DTO types not found, skipping");
                return;
            }

            // Find custom application service interface and implementation
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

            // Case 1: Both interface and implementation exist (generate partial class)
            if (serviceInterface != null && serviceImplementation != null)
            {
                if (serviceInterface.IsAssignableFrom(serviceImplementation))
                {
                    services.AddScoped(serviceInterface, serviceImplementation);
                    
                    // Generate partial class file
                    if (generatePartialClasses)
                    {
                        // Find custom repository interface
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
                        Console.WriteLine($"✅ {entityName}: Custom service registered and partial class generated {serviceInterfaceName} -> {serviceImplementationName}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ {entityName}: Custom service registered {serviceInterfaceName} -> {serviceImplementationName}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️  {entityName}: {serviceImplementationName} does not implement {serviceInterfaceName}");
                }
                return;
            }

            // Case 2: Only interface exists, no implementation (generate default implementation) exists, no implementation (generate default implementation)
            if (serviceInterface != null && serviceImplementation == null)
            {
                RegisterGenericAppService(services, entityType, dtoTypes, serviceInterface);
                Console.WriteLine($"🔧 {entityName}: Using generic service implementation {serviceInterfaceName}");
                return;
            }

            // Case 3: No custom interface or implementation (use default generic service)
            RegisterGenericAppService(services, entityType, dtoTypes, null);
            Console.WriteLine($"📦 {entityName}: Registered generic service IAppService<{entityName}, ...>");
        }

        /// <summary>
        /// Find DTO types corresponding to the entity
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

            // At least BaseDto and CreateDto are required
            if (baseDto == null || createDto == null)
            {
                return null;
            }

            // Use default types to fill in missing DTOs
            return new DtoTypes
            {
                BaseDto = baseDto,
                CreateDto = createDto,
                UpdateDto = updateDto ?? createDto, // If no UpdateDto, use CreateDto
                QueryDto = queryDto ?? typeof(QueryDto<>).MakeGenericType(baseDto.BaseType?.GetGenericArguments()[0] ?? typeof(object)),
                BasesDto = basesDto ?? baseDto // If no BasesDto, use BaseDto
            };
        }

        /// <summary>
        /// Register generic application service
        /// </summary>
        private static void RegisterGenericAppService(
            IServiceCollection services,
            Type entityType,
            DtoTypes dtoTypes,
            Type? customInterface)
        {
            // Build generic service type
            var serviceType = typeof(AppService<,,,,,>).MakeGenericType(
                entityType,
                dtoTypes.BaseDto,
                dtoTypes.CreateDto,
                dtoTypes.UpdateDto,
                dtoTypes.QueryDto,
                dtoTypes.BasesDto);

            // Build interface type
            var interfaceType = customInterface ?? typeof(IAppService<,,,,,>).MakeGenericType(
                entityType,
                dtoTypes.BaseDto,
                dtoTypes.CreateDto,
                dtoTypes.UpdateDto,
                dtoTypes.QueryDto,
                dtoTypes.BasesDto);

            // Register service
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
        /// Generate partial service class file
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

            // Get custom repository namespace and name
            string? customRepositoryNamespace = customRepositoryInterface?.Namespace;
            string? customRepositoryName = customRepositoryInterface?.Name;
            bool hasCustomRepository = customRepositoryInterface != null;

            // Build partial class code
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("// This file is auto-generated by AppServiceAutoRegistration");
            sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
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
            
            // If there's a custom repository, add its namespace
            if (hasCustomRepository && !string.IsNullOrEmpty(customRepositoryNamespace))
            {
                sb.AppendLine($"using {customRepositoryNamespace};");
            }
            
            sb.AppendLine();
            sb.AppendLine("namespace NextAdmin.Application.Services");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {entityName} Service - Auto-generated partial class");
            sb.AppendLine($"    /// Contains basic CRUD operations");
            if (hasCustomRepository)
            {
                sb.AppendLine($"    /// Uses custom repository: {customRepositoryName}");
            }
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public partial class {serviceClassName}");
            sb.AppendLine("    {");
            
            // Add custom repository field reference
            if (hasCustomRepository)
            {
                sb.AppendLine($"        // Custom repository reference (injected via constructor)");
                sb.AppendLine($"        // private readonly {customRepositoryName} _customRepository;");
                sb.AppendLine($"        // You can get custom repository via:");
                sb.AppendLine($"        // var customRepo = _baseRepository as {customRepositoryName};");
                sb.AppendLine($"        // Or inject in constructor: {customRepositoryName} customRepository");
                sb.AppendLine();
            }
            
            sb.AppendLine("        // This partial class is auto-generated by the system and contains basic CRUD functionality");
            sb.AppendLine("        // You can add custom business logic in another partial class file");
            sb.AppendLine();
            sb.AppendLine("        #region Auto-generated basic methods");
            sb.AppendLine();
            
            // Generate basic CRUD method hint comments
            sb.AppendLine("        // Basic CRUD methods are provided by the AppService base class:");
            sb.AppendLine("        // - Task<TBaseDto?> AddAsync(TCreateDto dto)");
            sb.AppendLine("        // - Task<TBaseDto?> UpdateAsync(TUpdateDto dto)");
            sb.AppendLine("        // - Task<bool> DeleteAsync(string id)");
            sb.AppendLine("        // - Task<TBaseDto?> GetAsync(string id)");
            sb.AppendLine("        // - Task<List<TBasesDto>> GetsAsync(Expression<Func<TEntity, bool>>? filter = null)");
            sb.AppendLine("        // - Task<QueryPageResultDto<TBasesDto>> GetListPageAsync(TQueryDto queryDto)");
            sb.AppendLine();
            
            // Add some helper method examples
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Check if {entityName} exists");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public async Task<bool> ExistsAsync(string id)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (string.IsNullOrEmpty(id)) return false;");
            sb.AppendLine("            var entity = await _baseRepository.GetAsync(e => e.Id == id);");
            sb.AppendLine("            return entity != null;");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Batch get {entityName}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public async Task<List<TBasesDto>> GetByIdsAsync(IEnumerable<string> ids)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (ids == null || !ids.Any()) return new List<TBasesDto>();");
            sb.AppendLine("            var entities = await _baseRepository.GetsAsync(e => ids.Contains(e.Id));");
            sb.AppendLine("            return Mapper.Map<List<TBasesDto>>(entities);");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Get enabled {entityName} list");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public async Task<List<TBasesDto>> GetEnabledAsync()");
            sb.AppendLine("        {");
            sb.AppendLine("            return await GetsAsync(e => e.IsEnabled);");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Batch enable/disable {entityName}");
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
            
            // If there's a custom repository, add helper methods to get the custom repository
            if (hasCustomRepository)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Get custom repository {customRepositoryName}");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        protected {customRepositoryName}? GetCustomRepository()");
                sb.AppendLine("        {");
                sb.AppendLine($"            return _baseRepository as {customRepositoryName};");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Get custom repository {customRepositoryName} (forced cast, will throw exception if fails)");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        protected {customRepositoryName} GetCustomRepositoryOrThrow()");
                sb.AppendLine("        {");
                sb.AppendLine($"            return (_baseRepository as {customRepositoryName}) ?? ");
                sb.AppendLine($"                throw new InvalidOperationException(\"Cannot convert _baseRepository to {customRepositoryName}\");");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
            
            sb.AppendLine("        #endregion");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            // Write to file
            try
            {
                File.WriteAllText(filePath, sb.ToString());
                Console.WriteLine($"   📄 Generated partial class: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  Failed to generate partial class: {ex.Message}");
            }
        }

        /// <summary>
        /// Print registered application service list (for debugging)
        /// </summary>
        public static void PrintRegisteredAppServices(this IServiceCollection services)
        {
            Console.WriteLine();
            Console.WriteLine("=== Registered Application Services List ===");

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

            Console.WriteLine($"📊Total {appServices.Count} application services registered");
            Console.WriteLine();
        }

        /// <summary>
        /// DTO types collection
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
