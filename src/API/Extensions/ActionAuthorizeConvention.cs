using NextAdmin.API.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace NextAdmin.API.Extensions
{
    /// <summary>
    /// 一个操作模型约定，用于为所有继承自泛型BaseController的控制器的Action自动应用授权策略。
    /// </summary>
    public class ActionAuthorizeConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            var controller = action.Controller;

            // 只对所有继承自泛型 BaseController<,,,,,> 的控制器生效（支持多层继承）
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

            // 获取实体名
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
