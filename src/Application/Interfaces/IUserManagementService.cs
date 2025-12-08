using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Auths;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.DTOs.Roles;
using NextAdmin.Application.DTOs.Users;
using NextAdmin.Shared.Common;

namespace NextAdmin.Application.Interfaces;

/// <summary>
/// User management service interface
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Get user list
    /// </summary>
    Task<PagedResultDto<UsersDto>> GetUsersAsync(UserQueryDto userQueryDto, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(string id);

    /// <summary>
    /// Get user by username
    /// </summary>
    Task<UserDto?> GetUserByUserNameAsync(string userName);

    /// <summary>
    /// Create user
    /// </summary>
    Task<Result<UserDto>> CreateUserAsync(CreateUserDto createUserDto);

    /// <summary>
    /// Update user
    /// </summary>
    Task<Result<UserDto>> UpdateUserAsync(string id, UpdateUserDto updateUserDto);

    /// <summary>
    /// Delete user
    /// </summary>
    Task<Result> DeleteUserAsync(string id);

    /// <summary>
    /// Change password
    /// </summary>
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);

    /// <summary>
    /// Reset password
    /// </summary>
    Task<Result<string>> ResetPasswordAsync(string userId);

    /// <summary>
    /// Activate/Deactivate user
    /// </summary>
    Task<Result> ToggleUserStatusAsync(string userId);

    /// <summary>
    /// Assign roles to user
    /// </summary>
    Task<Result> AssignRolesToUserAsync(string userId, List<string> roleIds);

    /// <summary>
    /// Get user's roles
    /// </summary>
    Task<List<RolesDto>> GetUserRolesAsync(string userId);
    Task<List<OptionDto>> GetOptionsAsync();
} 
