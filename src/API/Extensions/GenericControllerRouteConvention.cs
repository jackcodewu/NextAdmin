namespace NextAdmin.API.Extensions
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;

    public class GenericControllerRouteConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.IsGenericType &&
                controller.ControllerType.GetGenericTypeDefinition() == typeof(GenericController<,,,,,>))
            {
                var entityType = controller.ControllerType.GenericTypeArguments[0];

                var entityName = entityType.Name;
                controller.ControllerName = entityType.Name;

                // Add [Route] and [ApiController] attributes to controller
                var routeTemplate = $"api/{entityName}";
                controller.Selectors.Add(new SelectorModel
                {
                    AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(routeTemplate))
                });
                controller.Filters.Add(new ApiControllerAttribute());
            }
        }
    }
}
