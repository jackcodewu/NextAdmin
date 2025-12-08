namespace NextAdmin.API.Extensions
{
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using System.Reflection;

    public class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            // First get all existing manually written controller names
            var existingControllers = feature.Controllers.Select(c => c.Name).ToHashSet();

            // Automatically register all generic controllers
            foreach (var (entityType, baseDto, createDto, updateDto, queryDto, basesDto) in EntityDtoTypeHelper.GetAllEntityDtoTypes())
            {
                var conventionalControllerName = $"{entityType.Name}Controller";
                if (existingControllers.Contains(conventionalControllerName))
                {
                    // If exists, skip dynamically generating controller for this service
                    continue;
                }

                var controllerType = typeof(GenericController<,,,,,>)
                    .MakeGenericType(entityType, baseDto, createDto, updateDto, queryDto, basesDto)
                    .GetTypeInfo();

                // Avoid duplicate addition
                if (!feature.Controllers.Contains(controllerType))
                {
                    feature.Controllers.Add(controllerType);
                }
            }
        }
    }
}
