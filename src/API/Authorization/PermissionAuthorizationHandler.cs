using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NextAdmin.API.Authorization
{
    /// <summary>
    /// 处理 PermissionRequirement，检查用户是否拥有指定的权限声明。
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ILogger<PermissionAuthorizationHandler> _logger;

        public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        // 定义存储在JWT中的权限声明的类型。
        // 我们假设在用户登录并生成Token时，已将所有权限作为 "permission" 类型的 Claim 添加。
        public const string PermissionClaimType = "permission";

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var user = context.User;
            var requiredPermission = requirement.Permission;
            
            _logger.LogDebug("权限验证: 用户 {UserId} 需要权限 {Permission}", 
                user.Identity?.Name ?? "Unknown", requiredPermission);

            // 检查当前登录的用户是否拥有一个类型为 "permission" 且值与需求权限完全匹配的 Claim。
            if (context.User.HasClaim(PermissionClaimType, requiredPermission))
            {
                _logger.LogDebug("权限验证成功: 用户 {UserId} 拥有权限 {Permission}", 
                    user.Identity?.Name ?? "Unknown", requiredPermission);
                // 如果找到匹配的权限声明，则授权成功。
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("权限验证失败: 用户 {UserId} 缺少权限 {Permission}", 
                    user.Identity?.Name ?? "Unknown", requiredPermission);
                
                // 输出用户的所有权限声明用于调试
                var userPermissions = context.User.FindAll(PermissionClaimType);
                _logger.LogDebug("用户 {UserId} 拥有的权限: {Permissions}", 
                    user.Identity?.Name ?? "Unknown", 
                    string.Join(", ", userPermissions.Select(c => c.Value)));
            }
            // 如果没有找到，则不调用 Succeed，授权将默认失败。

            return Task.CompletedTask;
        }
    }
} 
