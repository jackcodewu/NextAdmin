using NextAdmin.API.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace NextAdmin.API.Extensions
{
    /// <summary>
    /// An action model convention that automatically applies authorization policies to all Actions of controllers inheriting from the generic BaseController.
    /// </summary>
    public class ActionAuthorizeConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            var controller = action.Controller;

            // Only effective for controllers inheriting from generic BaseController<,,,,,> (supports multi-level inheritance)
            var type = controller.ControllerType.AsType();
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BaseController<,,,,,>))
                {
                    break;
                }
                type = type.BaseType;
            }
            if (type == null)
            {
                return;
            }

            // Get entity name
            var entityName = type.GetGenericArguments().First().Name;

            var actionName = action.ActionName;
            if (actionName.EndsWith("Async"))
            {
                actionName = actionName.Substring(0, actionName.Length - 5);
            }

            string permissionSuffix;
            switch (actionName)
            {
                case "GetListPage":
                case "Get":
                    permissionSuffix = "View";
                    break;
                case "Create":
                    permissionSuffix = "Create";
                    break;
                case "Update":
                    permissionSuffix = "Edit";
                    break;
                case "Delete":
                    permissionSuffix = "Delete";
                    break;
                default:
                    return;
            }

            var policyName = $"{entityName}.{permissionSuffix}";
            action.Filters.Add(new AuthorizeFilter(policyName));
        }
    }
} 
