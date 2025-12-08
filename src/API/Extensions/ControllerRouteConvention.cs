using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using NextAdmin.API.Controllers;

namespace NextAdmin.API.Extensions
{
    public class ControllerRouteConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var controllerType = controller.ControllerType;

            // Only apply this convention to our dynamic generic controllers
            if (!controllerType.IsGenericType || controllerType.GetGenericTypeDefinition() != typeof(BaseController<,,,,,>))
            {
                return;
            }

            // Get entity name from generic parameters, e.g. 'Menu'
            var entityType = controllerType.GetGenericArguments().First();
            var entityName = entityType.Name;

            // Set controller name and remove "Controller" suffix
            controller.ControllerName = entityName;

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
