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
            // 先获取所有已存在的、手写的控制器名称
            var existingControllers = feature.Controllers.Select(c => c.Name).ToHashSet();

            // 查找所有实现了 IAppService<...> 接口的应用服务
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

                // 获取 IAppService<TEntity, TBaseDto, ...> 的泛型参数
                var genericArgs = interfaceType.GetGenericArguments();
                if (genericArgs.Length != 6)
                {
                    // 泛型参数数量不对，跳过
                    continue;
                }
                var entityType = genericArgs[0];
                var baseDtoType = genericArgs[1];
                var createDtoType = genericArgs[2];
                var updateDtoType = genericArgs[3];
                var queryDtoType = genericArgs[4];
                var keyType = genericArgs[5];

                // 检查是否已存在同名的手写控制器（例如 "MenuController"）
                var conventionalControllerName = $"{entityType.Name}Controller";
                if (existingControllers.Contains(conventionalControllerName))
                {
                    // 如果存在，则跳过为此服务动态生成控制器
                    continue;
                }

                // 跳过开放泛型类型
                if (entityType.IsGenericTypeDefinition || baseDtoType.IsGenericTypeDefinition || createDtoType.IsGenericTypeDefinition || updateDtoType.IsGenericTypeDefinition || queryDtoType.IsGenericTypeDefinition || keyType.IsGenericTypeDefinition)
                {
                    continue;
                }

                // 创建具体的泛型控制器类型, 如 BaseController<Menu, MenuDto, ...>
                Type closedControllerType = null;
                try
                {
                    closedControllerType = typeof(BaseController<,,,,,>)
                        .MakeGenericType(entityType, baseDtoType, createDtoType, updateDtoType, queryDtoType, keyType);
                }
                catch (ArgumentException)
                {
                    // 泛型参数不匹配，跳过
                    continue;
                }
                var controllerTypeInfo = closedControllerType.GetTypeInfo();

                // 如果该控制器尚未被发现，则添加
                if (!feature.Controllers.Any(c => c.AsType() == closedControllerType))
                {
                    feature.Controllers.Add(controllerTypeInfo);
                }
            }
        }
    }
} 
