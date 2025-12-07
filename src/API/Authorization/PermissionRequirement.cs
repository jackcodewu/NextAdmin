using Microsoft.AspNetCore.Authorization;

namespace NextAdmin.API.Authorization
{
    /// <summary>
    /// 定义一个授权需求，它要求用户必须拥有特定的权限。
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// 所需的权限字符串，例如 "Menu.View".
        /// </summary>
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
} 
