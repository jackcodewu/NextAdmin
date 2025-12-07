using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace NextAdmin.Infrastructure.Extensions
{
    /// <summary>
    /// 仓储自动注册提供器
    /// 自动扫描并注册所有继承 AggregateRoot 的实体类的仓储
    /// </summary>
    public static class RepositoryAutoRegistration
    {
        /// <summary>
        /// 自动注册所有仓储
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="assemblies">要扫描的程序集列表</param>
        public static IServiceCollection AddAutoRepositories(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                // 如果没有指定程序集，默认扫描 Core 和 Infrastructure 程序集
                assemblies = new[]
                {
                    Assembly.Load("NextAdmin.Core"),
                    Assembly.Load("NextAdmin.Infrastructure")
                };
            }

            // 1. 查找所有继承 AggregateRoot 的实体类
            var entityTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => 
                    type.IsClass && 
                    !type.IsAbstract && 
                    !type.IsGenericTypeDefinition &&
                    typeof(AggregateRoot).IsAssignableFrom(type))
                .ToList();

            Console.WriteLine($"[RepositoryAutoRegistration] 发现 {entityTypes.Count} 个实体类");

            // 2. 为每个实体类注册仓储
            foreach (var entityType in entityTypes)
            {
                RegisterRepositoryForEntity(services, entityType, assemblies);
            }

            // 3. 注册通用仓储
            services.AddScoped(typeof(IBaseRepository<>), typeof(Repositories.BaseRepository<>));
            
            return services;
        }

        /// <summary>
        /// 为单个实体类注册仓储
        /// </summary>
        private static void RegisterRepositoryForEntity(
            IServiceCollection services, 
            Type entityType,
            Assembly[] assemblies)
        {
            // 构建仓储接口名称，例如：IWordRepository, IMenuRepository
            var repositoryInterfaceName = $"I{entityType.Name}Repository";
            
            // 构建仓储实现类名称，例如：WordRepository, MenuRepository
            var repositoryImplementationName = $"{entityType.Name}Repository";

            // 在程序集中查找仓储接口
            var repositoryInterface = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type => 
                    type.IsInterface && 
                    type.Name == repositoryInterfaceName);

            // 在程序集中查找仓储实现类
            var repositoryImplementation = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type => 
                    type.IsClass && 
                    !type.IsAbstract &&
                    type.Name == repositoryImplementationName);

            // 如果同时找到接口和实现类，则注册
            if (repositoryInterface != null && repositoryImplementation != null)
            {
                // 验证实现类是否实现了接口
                if (repositoryInterface.IsAssignableFrom(repositoryImplementation))
                {
                    services.AddScoped(repositoryInterface, repositoryImplementation);
                    Console.WriteLine($"[RepositoryAutoRegistration] ✅ 已注册: {repositoryInterface.Name} -> {repositoryImplementation.Name}");
                }
                else
                {
                    Console.WriteLine($"[RepositoryAutoRegistration] ⚠️  {repositoryImplementation.Name} 未实现 {repositoryInterface.Name}");
                }
            }
            else
            {
                // 如果没有找到自定义仓储，使用泛型 IBaseRepository<T>
                var baseRepositoryInterface = typeof(IBaseRepository<>).MakeGenericType(entityType);
                var baseRepositoryImplementation = typeof(Repositories.BaseRepository<>).MakeGenericType(entityType);
                
                // 检查是否已经注册过
                if (!services.Any(sd => sd.ServiceType == baseRepositoryInterface))
                {
                    services.AddScoped(baseRepositoryInterface, baseRepositoryImplementation);
                    Console.WriteLine($"[RepositoryAutoRegistration] 📦 使用泛型仓储: IBaseRepository<{entityType.Name}>");
                }
            }
        }

        /// <summary>
        /// 手动注册单个仓储（用于替换自动注册）
        /// </summary>
        public static IServiceCollection AddRepository<TInterface, TImplementation>(
            this IServiceCollection services)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            // 移除可能存在的自动注册
            var existingDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(TInterface));
            if (existingDescriptor != null)
            {
                services.Remove(existingDescriptor);
                Console.WriteLine($"[RepositoryAutoRegistration] 🔄 替换自动注册: {typeof(TInterface).Name}");
            }

            services.AddScoped<TInterface, TImplementation>();
            Console.WriteLine($"[RepositoryAutoRegistration] ✅ 手动注册: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            
            return services;
        }

        /// <summary>
        /// 获取所有已注册的仓储信息（用于调试）
        /// </summary>
        public static void PrintRegisteredRepositories(this IServiceCollection services)
        {
            Console.WriteLine("\n========== 已注册的仓储列表 ==========");
            
            var repositories = services
                .Where(sd => sd.ServiceType.IsGenericType && 
                            sd.ServiceType.GetGenericTypeDefinition() == typeof(IBaseRepository<>) ||
                            sd.ServiceType.Name.EndsWith("Repository"))
                .ToList();

            foreach (var repo in repositories)
            {
                var serviceType = repo.ServiceType.Name;
                var implementationType = repo.ImplementationType?.Name ?? "Factory/Instance";
                Console.WriteLine($"  {serviceType} -> {implementationType}");
            }
            
            Console.WriteLine($"总计: {repositories.Count} 个仓储");
            Console.WriteLine("=====================================\n");
        }
    }
}
