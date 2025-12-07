using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Auths;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.DTOs.Roles;
using NextAdmin.Application.DTOs.Users;
using NextAdmin.Shared.Common;

namespace NextAdmin.Application.Interfaces;

/// <summary>
/// 用户管理服务接口
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// 获取用户列表
    /// </summary>
    Task<PagedResultDto<UsersDto>> GetUsersAsync(UserQueryDto userQueryDto, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(string id);

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<UserDto?> GetUserByUserNameAsync(string userName);

    /// <summary>
    /// 创建用户
    /// </summary>
    Task<Result<UserDto>> CreateUserAsync(CreateUserDto createUserDto);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task<Result<UserDto>> UpdateUserAsync(string id, UpdateUserDto updateUserDto);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task<Result> DeleteUserAsync(string id);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);

    /// <summary>
    /// 重置密码
    /// </summary>
    Task<Result<string>> ResetPasswordAsync(string userId);

    /// <summary>
    /// 激活/停用用户
    /// </summary>
    Task<Result> ToggleUserStatusAsync(string userId);

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    Task<Result> AssignRolesToUserAsync(string userId, List<string> roleIds);

    /// <summary>
    /// 获取用户的角色
    /// </summary>
    Task<List<RolesDto>> GetUserRolesAsync(string userId);
    Task<List<OptionDto>> GetOptionsAsync();
} 
