using Microsoft.AspNetCore.Authorization;

namespace NextAdmin.API.Authorization
{
    /// <summary>
    /// Defines an authorization requirement that requires the user to have a specific permission.
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Required permission string, e.g. "Menu.View".
        /// </summary>
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
} 
