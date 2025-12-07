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

            // 仅对我们的动态泛型控制器应用此约定
            if (!controllerType.IsGenericType || controllerType.GetGenericTypeDefinition() != typeof(BaseController<,,,,,>))
            {
                return;
            }

            // 从泛型参数中获取实体名称, 如 'Menu'
            var entityType = controllerType.GetGenericArguments().First();
            var entityName = entityType.Name;

            // 设置控制器名称，并移除 "Controller" 后缀
            controller.ControllerName = entityName;

            // 为控制器添加 [Route] 和 [ApiController] 特性
            var routeTemplate = $"api/{entityName}";
            controller.Selectors.Add(new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(routeTemplate))
            });
            controller.Filters.Add(new ApiControllerAttribute());
        }
    }
} 
