using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NextAdmin.API.Authorization
{
    /// <summary>
    /// Handles PermissionRequirement, checks if user has the specified permission claim.
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ILogger<PermissionAuthorizationHandler> _logger;

        public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        // Define the type of permission claim stored in JWT.
        // We assume that when user logs in and generates Token, all permissions have been added as "permission" type Claims.
        public const string PermissionClaimType = "permission";

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var user = context.User;
            var requiredPermission = requirement.Permission;
            
            _logger.LogDebug("Permission verification: User {UserId} requires permission {Permission}", 
                user.Identity?.Name ?? "Unknown", requiredPermission);

            // Check if the currently logged-in user has a Claim of type "permission" with a value that exactly matches the required permission.
            if (context.User.HasClaim(PermissionClaimType, requiredPermission))
            {
                _logger.LogDebug("Permission verification successful: User {UserId} has permission {Permission}", 
                    user.Identity?.Name ?? "Unknown", requiredPermission);
                // If matching permission claim is found, authorization succeeds.
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Permission verification failed: User {UserId} lacks permission {Permission}", 
                    user.Identity?.Name ?? "Unknown", requiredPermission);
                
                // Output user's all permission claims for debugging
                var userPermissions = context.User.FindAll(PermissionClaimType);
                _logger.LogDebug("User {UserId} has permissions: {Permissions}", 
                    user.Identity?.Name ?? "Unknown", 
                    string.Join(", ", userPermissions.Select(c => c.Value)));
            }
            // If not found, do not call Succeed, authorization will fail by default.

            return Task.CompletedTask;
        }
    }
} 
