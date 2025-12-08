namespace NextAdmin.API.Extensions
{
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;
    using Microsoft.AspNetCore.Mvc.Authorization;

    public class DynamicAuthorizeConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.IsGenericType &&
                controller.ControllerType.GetGenericTypeDefinition() == typeof(GenericController<,,,,,>))
            {
                var entityType = controller.ControllerType.GenericTypeArguments[0];
                var entityName = entityType.Name;

                // Add Policy dynamically for each Action
                foreach (var action in controller.Actions)
                {
                    string policy = action.ActionName switch
                    {
                        "GetAsync" => $"{entityName}.View",
                        "GetListPageAsync" => $"{entityName}.View",
                        "GetAllAsync" => $"{entityName}.View",
                        "GetOptionsAsync" => $"{entityName}.View",
                        "CreateAsync" => $"{entityName}.Create",
                        "UpdateAsync" => $"{entityName}.Edit",
                        "DeleteAsync" => $"{entityName}.Delete",
                        _ => $"{entityName}.View"
                    };

                    action.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser().RequireClaim("permission", policy).Build()));
                }
            }
        }
    }

}
