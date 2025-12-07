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
    /// 动态仓储生成器
    /// 在运行时动态生成缺失的仓储实现类
    /// </summary>
    public static class DynamicRepositoryGenerator
    {
        private static readonly ModuleBuilder _moduleBuilder;
        private static readonly Dictionary<Type, Type> _generatedTypes = new();

        static DynamicRepositoryGenerator()
        {
            // 创建动态程序集
            var assemblyName = new AssemblyName("DynamicRepositories");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, 
                AssemblyBuilderAccess.Run);
            
            _moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        }

        /// <summary>
        /// 为实体动态生成仓储类
        /// </summary>
        public static Type GenerateRepositoryType(Type entityType, Type interfaceType)
        {
            // 检查缓存
            if (_generatedTypes.TryGetValue(entityType, out var cachedType))
            {
                return cachedType;
            }

            var repositoryName = $"{entityType.Name}Repository_Dynamic";
            
            Console.WriteLine($"[DynamicRepositoryGenerator] 🔧 动态生成: {repositoryName}");

            // 创建类型构建器
            var typeBuilder = _moduleBuilder.DefineType(
                repositoryName,
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(BaseRepository<>).MakeGenericType(entityType));

            // 实现接口
            typeBuilder.AddInterfaceImplementation(interfaceType);

            // 生成构造函数
            GenerateConstructor(typeBuilder, entityType);

            // 实现接口方法
            GenerateInterfaceMethods(typeBuilder, interfaceType, entityType);

            // 创建类型
            var generatedType = typeBuilder.CreateType();
            
            // 缓存生成的类型
            _generatedTypes[entityType] = generatedType!;

            Console.WriteLine($"[DynamicRepositoryGenerator] ✅ 已生成: {repositoryName}");

            return generatedType!;
        }

        /// <summary>
        /// 生成构造函数
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
                throw new InvalidOperationException($"找不到 BaseRepository<{entityType.Name}> 的构造函数");

            // 定义构造函数：public {Repository}(IMongoDatabase database, IRedisService redisService)
            var constructor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(IMongoDatabase), typeof(Redis.IRedisService) });

            var ilGenerator = constructor.GetILGenerator();

            // 调用基类构造函数
            ilGenerator.Emit(OpCodes.Ldarg_0); // this
            ilGenerator.Emit(OpCodes.Ldarg_1); // database
            ilGenerator.Emit(OpCodes.Ldarg_2); // redisService
            ilGenerator.Emit(OpCodes.Call, baseConstructor);
            ilGenerator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// 实现接口方法（委托给基类）
        /// </summary>
        private static void GenerateInterfaceMethods(TypeBuilder typeBuilder, Type interfaceType, Type entityType)
        {
            var baseRepositoryType = typeof(IBaseRepository<>).MakeGenericType(entityType);
            
            // 获取接口中定义的所有方法（排除从 IBaseRepository 继承的）
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
        /// 生成单个方法实现
        /// </summary>
        private static void GenerateMethod(TypeBuilder typeBuilder, MethodInfo methodInfo, Type entityType)
        {
            var parameters = methodInfo.GetParameters();
            var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

            // 定义方法
            var methodBuilder = typeBuilder.DefineMethod(
                methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                methodInfo.ReturnType,
                parameterTypes);

            var ilGenerator = methodBuilder.GetILGenerator();

            // 生成方法体：抛出 NotImplementedException
            // 这样可以运行，但调用自定义方法时会提示未实现
            var notImplementedCtor = typeof(NotImplementedException).GetConstructor(
                new[] { typeof(string) });

            ilGenerator.Emit(OpCodes.Ldstr, 
                $"方法 {methodInfo.Name} 需要手动实现。请创建 {entityType.Name}Repository 类。");
            ilGenerator.Emit(OpCodes.Newobj, notImplementedCtor!);
            ilGenerator.Emit(OpCodes.Throw);
        }

        /// <summary>
        /// 检查方法签名是否匹配
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
        /// 自动注册所有仓储（支持动态生成）
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

            // 查找所有继承 AggregateRoot 的实体类
            var entityTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    !type.IsGenericTypeDefinition &&
                    typeof(AggregateRoot).IsAssignableFrom(type))
                .ToList();

            Console.WriteLine($"[DynamicRepositoryGenerator] 发现 {entityTypes.Count} 个实体类");

            foreach (var entityType in entityTypes)
            {
                RegisterRepositoryWithDynamicGeneration(services, entityType, assemblies);
            }

            // 注册通用仓储
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            return services;
        }

        /// <summary>
        /// 注册单个仓储（支持动态生成）
        /// </summary>
        private static void RegisterRepositoryWithDynamicGeneration(
            IServiceCollection services,
            Type entityType,
            Assembly[] assemblies)
        {
            var repositoryInterfaceName = $"I{entityType.Name}Repository";
            var repositoryImplementationName = $"{entityType.Name}Repository";

            // 查找仓储接口
            var repositoryInterface = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type =>
                    type.IsInterface &&
                    type.Name == repositoryInterfaceName);

            // 查找仓储实现类
            var repositoryImplementation = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    type.Name == repositoryImplementationName);

            if (repositoryInterface != null && repositoryImplementation != null)
            {
                // 情况 1：接口和实现类都存在
                if (repositoryInterface.IsAssignableFrom(repositoryImplementation))
                {
                    services.AddScoped(repositoryInterface, repositoryImplementation);
                    Console.WriteLine($"[DynamicRepositoryGenerator] ✅ 已注册: {repositoryInterface.Name} -> {repositoryImplementation.Name}");
                }
            }
            else if (repositoryInterface != null && repositoryImplementation == null)
            {
                // 情况 2：接口存在但实现类不存在 → 动态生成
                try
                {
                    var dynamicType = GenerateRepositoryType(entityType, repositoryInterface);
                    services.AddScoped(repositoryInterface, dynamicType);
                    Console.WriteLine($"[DynamicRepositoryGenerator] 🔧 动态生成并注册: {repositoryInterface.Name} -> {dynamicType.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DynamicRepositoryGenerator] ❌ 动态生成失败: {repositoryInterface.Name} - {ex.Message}");
                    
                    // 回退到泛型仓储
                    var baseRepositoryInterface = typeof(IBaseRepository<>).MakeGenericType(entityType);
                    var baseRepositoryImplementation = typeof(BaseRepository<>).MakeGenericType(entityType);
                    
                    if (!services.Any(sd => sd.ServiceType == baseRepositoryInterface))
                    {
                        services.AddScoped(baseRepositoryInterface, baseRepositoryImplementation);
                        Console.WriteLine($"[DynamicRepositoryGenerator] 📦 回退到泛型仓储: IBaseRepository<{entityType.Name}>");
                    }
                }
            }
            else
            {
                // 情况 3：没有自定义接口 → 使用泛型仓储
                var baseRepositoryInterface = typeof(IBaseRepository<>).MakeGenericType(entityType);
                var baseRepositoryImplementation = typeof(BaseRepository<>).MakeGenericType(entityType);

                if (!services.Any(sd => sd.ServiceType == baseRepositoryInterface))
                {
                    services.AddScoped(baseRepositoryInterface, baseRepositoryImplementation);
                    Console.WriteLine($"[DynamicRepositoryGenerator] 📦 使用泛型仓储: IBaseRepository<{entityType.Name}>");
                }
            }
        }
    }
}
