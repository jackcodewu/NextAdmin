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
    /// Repository auto-registration provider
    /// Automatically scan and register repositories for all entities inheriting AggregateRoot
    /// </summary>
    public static class RepositoryAutoRegistration
    {
        /// <summary>
        /// Auto-register all repositories
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">List of assemblies to scan</param>
        public static IServiceCollection AddAutoRepositories(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                // If no assemblies specified, default to scanning Core and Infrastructure assemblies
                assemblies = new[]
                {
                    Assembly.Load("NextAdmin.Core"),
                    Assembly.Load("NextAdmin.Infrastructure")
                };
            }

            // 1. Find all entity classes inheriting AggregateRoot
            var entityTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => 
                    type.IsClass && 
                    !type.IsAbstract && 
                    !type.IsGenericTypeDefinition &&
                    typeof(AggregateRoot).IsAssignableFrom(type))
                .ToList();

            Console.WriteLine($"[RepositoryAutoRegistration] Found {entityTypes.Count} entity classes");

            // 2. Register repository for each entity class
            foreach (var entityType in entityTypes)
            {
                RegisterRepositoryForEntity(services, entityType, assemblies);
            }

            // 3. Register generic repository
            services.AddScoped(typeof(IBaseRepository<>), typeof(Repositories.BaseRepository<>));
            
            return services;
        }

        /// <summary>
        /// Register repository for a single entity class
        /// </summary>
        private static void RegisterRepositoryForEntity(
            IServiceCollection services, 
            Type entityType,
            Assembly[] assemblies)
        {
            // Build repository interface name, e.g.: IWordRepository, IMenuRepository
            var repositoryInterfaceName = $"I{entityType.Name}Repository";
            
            // Build repository implementation class name, e.g.: WordRepository, MenuRepository
            var repositoryImplementationName = $"{entityType.Name}Repository";

            // Find repository interface in assemblies
            var repositoryInterface = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type => 
                    type.IsInterface && 
                    type.Name == repositoryInterfaceName);

            // Find repository implementation class in assemblies
            var repositoryImplementation = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type => 
                    type.IsClass && 
                    !type.IsAbstract &&
                    type.Name == repositoryImplementationName);

            // If both interface and implementation class are found, register them
            if (repositoryInterface != null && repositoryImplementation != null)
            {
                // Verify that implementation class implements the interface
                if (repositoryInterface.IsAssignableFrom(repositoryImplementation))
                {
                    services.AddScoped(repositoryInterface, repositoryImplementation);
                    Console.WriteLine($"[RepositoryAutoRegistration] ✅ Registered: {repositoryInterface.Name} -> {repositoryImplementation.Name}");
                }
                else
                {
                    Console.WriteLine($"[RepositoryAutoRegistration] ⚠️  {repositoryImplementation.Name} does not implement {repositoryInterface.Name}");
                }
            }
            else
            {
                // If custom repository not found, use generic IBaseRepository<T>
                var baseRepositoryInterface = typeof(IBaseRepository<>).MakeGenericType(entityType);
                var baseRepositoryImplementation = typeof(Repositories.BaseRepository<>).MakeGenericType(entityType);
                
                // Check if already registered
                if (!services.Any(sd => sd.ServiceType == baseRepositoryInterface))
                {
                    services.AddScoped(baseRepositoryInterface, baseRepositoryImplementation);
                    Console.WriteLine($"[RepositoryAutoRegistration] 📦 Use generic repository: IBaseRepository<{entityType.Name}>");
                }
            }
        }

        /// <summary>
        /// Manually register a single repository (to replace auto-registration)
        /// </summary>
        public static IServiceCollection AddRepository<TInterface, TImplementation>(
            this IServiceCollection services)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            // Remove existing auto-registration
            var existingDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(TInterface));
            if (existingDescriptor != null)
            {
                services.Remove(existingDescriptor);
                Console.WriteLine($"[RepositoryAutoRegistration] 🔄 Replace auto-registration: {typeof(TInterface).Name}");
            }

            services.AddScoped<TInterface, TImplementation>();
            Console.WriteLine($"[RepositoryAutoRegistration] ✅ Manually registered: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            
            return services;
        }

        /// <summary>
        /// Get all registered repository information (for debugging)
        /// </summary>
        public static void PrintRegisteredRepositories(this IServiceCollection services)
        {
            Console.WriteLine("\n========== Registered Repository List ==========");
            
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
            
            Console.WriteLine($"Total: {repositories.Count} repositories");
            Console.WriteLine("=====================================\n");
        }
    }
}
