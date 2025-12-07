namespace NextAdmin.API.Extensions
{
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using System.Reflection;

    public class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            // 先获取所有已存在的、手写的控制器名称
            var existingControllers = feature.Controllers.Select(c => c.Name).ToHashSet();

            // 自动注册所有泛型控制器
            foreach (var (entityType, baseDto, createDto, updateDto, queryDto, basesDto) in EntityDtoTypeHelper.GetAllEntityDtoTypes())
            {
                var conventionalControllerName = $"{entityType.Name}Controller";
                if (existingControllers.Contains(conventionalControllerName))
                {
                    // 如果存在，则跳过为此服务动态生成控制器
                    continue;
                }

                var controllerType = typeof(GenericController<,,,,,>)
                    .MakeGenericType(entityType, baseDto, createDto, updateDto, queryDto, basesDto)
                    .GetTypeInfo();

                // 避免重复添加
                if (!feature.Controllers.Contains(controllerType))
                {
                    feature.Controllers.Add(controllerType);
                }
            }
        }
    }
}
