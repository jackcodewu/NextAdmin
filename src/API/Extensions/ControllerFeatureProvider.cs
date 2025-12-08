using NextAdmin.API.Controllers;
using NextAdmin.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;

namespace NextAdmin.API.Extensions
{
    public class ControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            // First get all existing manually written controller names
            var existingControllers = feature.Controllers.Select(c => c.Name).ToHashSet();

            // Find all application services that implement the IAppService<...> interface
            var appServiceTypes = parts
                .OfType<IApplicationPartTypeProvider>()
                .SelectMany(p => p.Types)
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition && t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IAppService<,,,,,>))) // Note the generic arity
                .ToList();

            foreach (var appServiceType in appServiceTypes)
            {
                var interfaceType = appServiceType.GetInterfaces().First(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IAppService<,,,,,>));

                // Get IAppService<TEntity, TBaseDto, ...> generic parameters
                var genericArgs = interfaceType.GetGenericArguments();
                if (genericArgs.Length != 6)
                {
                    // Wrong number of generic parameters, skip
                    continue;
                }
                var entityType = genericArgs[0];
                var baseDtoType = genericArgs[1];
                var createDtoType = genericArgs[2];
                var updateDtoType = genericArgs[3];
                var queryDtoType = genericArgs[4];
                var keyType = genericArgs[5];

                // Check if manually written controller with same name exists (e.g. "MenuController")
                var conventionalControllerName = $"{entityType.Name}Controller";
                if (existingControllers.Contains(conventionalControllerName))
                {
                    // If exists, skip dynamically generating controller for this service
                    continue;
                }

                // Skip open generic types
                if (entityType.IsGenericTypeDefinition || baseDtoType.IsGenericTypeDefinition || createDtoType.IsGenericTypeDefinition || updateDtoType.IsGenericTypeDefinition || queryDtoType.IsGenericTypeDefinition || keyType.IsGenericTypeDefinition)
                {
                    continue;
                }

                // Create specific generic controller type, e.g. BaseController<Menu, MenuDto, ...>
                Type closedControllerType = null;
                try
                {
                    closedControllerType = typeof(BaseController<,,,,,>)
                        .MakeGenericType(entityType, baseDtoType, createDtoType, updateDtoType, queryDtoType, keyType);
                }
                catch (ArgumentException)
                {
                    // Generic parameters don't match, skip
                    continue;
                }
                var controllerTypeInfo = closedControllerType.GetTypeInfo();

                // If controller hasn't been discovered yet, add it
                if (!feature.Controllers.Any(c => c.AsType() == closedControllerType))
                {
                    feature.Controllers.Add(controllerTypeInfo);
                }
            }
        }
    }
} 
