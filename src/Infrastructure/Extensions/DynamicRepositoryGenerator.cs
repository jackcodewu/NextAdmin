using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using NextAdmin.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace NextAdmin.Infrastructure.Extensions
{
    /// <summary>
    /// Dynamic repository generator
    /// Dynamically generates missing repository implementation classes at runtime
    /// </summary>
    public static class DynamicRepositoryGenerator
    {
        private static readonly ModuleBuilder _moduleBuilder;
        private static readonly Dictionary<Type, Type> _generatedTypes = new();

        static DynamicRepositoryGenerator()
        {
            // Create dynamic assembly
            var assemblyName = new AssemblyName("DynamicRepositories");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, 
                AssemblyBuilderAccess.Run);
            
            _moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        }

        /// <summary>
        /// Dynamically generate repository type for entity
        /// </summary>
        public static Type GenerateRepositoryType(Type entityType, Type interfaceType)
        {
            // Check cache
            if (_generatedTypes.TryGetValue(entityType, out var cachedType))
            {
                return cachedType;
            }

            var repositoryName = $"{entityType.Name}Repository_Dynamic";
            
            Console.WriteLine($"[DynamicRepositoryGenerator] 🔧 Dynamically generating: {repositoryName}");

            // Create type builder
            var typeBuilder = _moduleBuilder.DefineType(
                repositoryName,
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(BaseRepository<>).MakeGenericType(entityType));

            // Implement interface
            typeBuilder.AddInterfaceImplementation(interfaceType);

            // Generate constructor
            GenerateConstructor(typeBuilder, entityType);

            // Implement interface methods
            GenerateInterfaceMethods(typeBuilder, interfaceType, entityType);

            // Create type
            var generatedType = typeBuilder.CreateType();
            
            // Cache generated type
            _generatedTypes[entityType] = generatedType!;

            Console.WriteLine($"[DynamicRepositoryGenerator] ✅ Generated: {repositoryName}");

            return generatedType!;
        }

        /// <summary>
        /// Generate constructor
        /// </summary>
        private static void GenerateConstructor(TypeBuilder typeBuilder, Type entityType)
        {
            var baseType = typeof(BaseRepository<>).MakeGenericType(entityType);
            var baseConstructor = baseType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(IMongoDatabase), typeof(Redis.IRedisService) },
                null);

            if (baseConstructor == null)
                throw new InvalidOperationException($"Cannot find constructor of BaseRepository<{entityType.Name}>");

            // Define constructor: public {Repository}(IMongoDatabase database, IRedisService redisService)
            var constructor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(IMongoDatabase), typeof(Redis.IRedisService) });

            var ilGenerator = constructor.GetILGenerator();

            // Call base class constructor
            ilGenerator.Emit(OpCodes.Ldarg_0); // this
            ilGenerator.Emit(OpCodes.Ldarg_1); // database
            ilGenerator.Emit(OpCodes.Ldarg_2); // redisService
            ilGenerator.Emit(OpCodes.Call, baseConstructor);
            ilGenerator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Implement interface methods (delegate to base class)
        /// </summary>
        private static void GenerateInterfaceMethods(TypeBuilder typeBuilder, Type interfaceType, Type entityType)
        {
            var baseRepositoryType = typeof(IBaseRepository<>).MakeGenericType(entityType);
            
            // Get all methods defined in interface (excluding those inherited from IBaseRepository)
            var methods = interfaceType.GetMethods()
                .Where(m => !baseRepositoryType.GetMethods().Any(bm => 
                    bm.Name == m.Name && 
                    MethodSignaturesMatch(bm, m)))
                .ToList();

            foreach (var method in methods)
            {
                GenerateMethod(typeBuilder, method, entityType);
            }
        }

        /// <summary>
        /// Generate single method implementation
        /// </summary>
        private static void GenerateMethod(TypeBuilder typeBuilder, MethodInfo methodInfo, Type entityType)
        {
            var parameters = methodInfo.GetParameters();
            var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

            // Define method
            var methodBuilder = typeBuilder.DefineMethod(
                methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                methodInfo.ReturnType,
                parameterTypes);

            var ilGenerator = methodBuilder.GetILGenerator();

            // Generate method body: throw NotImplementedException
            // This allows it to run, but custom method calls will prompt as not implemented
            var notImplementedCtor = typeof(NotImplementedException).GetConstructor(
                new[] { typeof(string) });

            ilGenerator.Emit(OpCodes.Ldstr, 
                $"Method {methodInfo.Name} needs to be manually implemented. Please create {entityType.Name}Repository class.");
            ilGenerator.Emit(OpCodes.Newobj, notImplementedCtor!);
            ilGenerator.Emit(OpCodes.Throw);
        }

        /// <summary>
        /// Check if method signatures match
        /// </summary>
        private static bool MethodSignaturesMatch(MethodInfo method1, MethodInfo method2)
        {
            if (method1.Name != method2.Name)
                return false;

            var params1 = method1.GetParameters();
            var params2 = method2.GetParameters();

            if (params1.Length != params2.Length)
                return false;

            for (int i = 0; i < params1.Length; i++)
            {
                if (params1[i].ParameterType != params2[i].ParameterType)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Auto-register all repositories (supports dynamic generation)
        /// </summary>
        public static IServiceCollection AddAutoRepositoriesWithDynamicGeneration(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                assemblies = new[]
                {
                    Assembly.Load("NextAdmin.Core"),
                    Assembly.Load("NextAdmin.Infrastructure")
                };
            }

            // Find all entity classes inheriting AggregateRoot
            var entityTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    !type.IsGenericTypeDefinition &&
                    typeof(AggregateRoot).IsAssignableFrom(type))
                .ToList();

            Console.WriteLine($"[DynamicRepositoryGenerator] Found {entityTypes.Count} entity classes");

            foreach (var entityType in entityTypes)
            {
                RegisterRepositoryWithDynamicGeneration(services, entityType, assemblies);
            }

            // Register generic repository
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            return services;
        }

        /// <summary>
        /// Register single repository (supports dynamic generation)
        /// </summary>
        private static void RegisterRepositoryWithDynamicGeneration(
            IServiceCollection services,
            Type entityType,
            Assembly[] assemblies)
        {
            var repositoryInterfaceName = $"I{entityType.Name}Repository";
            var repositoryImplementationName = $"{entityType.Name}Repository";

            // Find repository interface
            var repositoryInterface = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type =>
                    type.IsInterface &&
                    type.Name == repositoryInterfaceName);

            // Find repository implementation class
            var repositoryImplementation = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    type.Name == repositoryImplementationName);

            if (repositoryInterface != null && repositoryImplementation != null)
            {
                // Case 1: Both interface and implementation class exist
                if (repositoryInterface.IsAssignableFrom(repositoryImplementation))
                {
                    services.AddScoped(repositoryInterface, repositoryImplementation);
                    Console.WriteLine($"[DynamicRepositoryGenerator] ✅ Registered: {repositoryInterface.Name} -> {repositoryImplementation.Name}");
                }
            }
            else if (repositoryInterface != null && repositoryImplementation == null)
            {
                // Case 2: Interface exists but implementation class doesn't → dynamically generate
                try
                {
                    var dynamicType = GenerateRepositoryType(entityType, repositoryInterface);
                    services.AddScoped(repositoryInterface, dynamicType);
                    Console.WriteLine($"[DynamicRepositoryGenerator] 🔧 Dynamically generated and registered: {repositoryInterface.Name} -> {dynamicType.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DynamicRepositoryGenerator] ❌ Dynamic generation failed: {repositoryInterface.Name} - {ex.Message}");
                    
                    // Fallback to generic repository
                    var baseRepositoryInterface = typeof(IBaseRepository<>).MakeGenericType(entityType);
                    var baseRepositoryImplementation = typeof(BaseRepository<>).MakeGenericType(entityType);
                    
                    if (!services.Any(sd => sd.ServiceType == baseRepositoryInterface))
                    {
                        services.AddScoped(baseRepositoryInterface, baseRepositoryImplementation);
                        Console.WriteLine($"[DynamicRepositoryGenerator] 📦 Fallback to generic repository: IBaseRepository<{entityType.Name}>");
                    }
                }
            }
            else
            {
                // Case 3: No custom interface → use generic repository
                var baseRepositoryInterface = typeof(IBaseRepository<>).MakeGenericType(entityType);
                var baseRepositoryImplementation = typeof(BaseRepository<>).MakeGenericType(entityType);

                if (!services.Any(sd => sd.ServiceType == baseRepositoryInterface))
                {
                    services.AddScoped(baseRepositoryInterface, baseRepositoryImplementation);
                    Console.WriteLine($"[DynamicRepositoryGenerator] 📦 Use generic repository: IBaseRepository<{entityType.Name}>");
                }
            }
        }
    }
}
