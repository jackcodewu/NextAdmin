using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.DTOs.Roles;
using NextAdmin.Application.DTOs.Users;
using NextAdmin.Shared.Common;

namespace NextAdmin.Application.Interfaces;

/// <summary>
/// 角色管理服务接口
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// 获取角色列表
    /// </summary>
    Task<PagedResultDto<RolesDto>> GetRolesAsync(RoleQueryDto roleQueryDto,int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// 获取所有角色（不分页）
    /// </summary>
    Task<List<RoleDto>> GetAllRolesAsync();

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    Task<RoleDto?> GetRoleByIdAsync(string id);

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    Task<RoleDto?> GetRoleByNameAsync(string name);

    /// <summary>
    /// 创建角色
    /// </summary>
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto);

    /// <summary>
    /// 更新角色
    /// </summary>
    Task<Result<RoleDto>> UpdateRoleAsync(string id, UpdateRoleDto updateRoleDto);

    /// <summary>
    /// 删除角色
    /// </summary>
    Task<Result> DeleteRoleAsync(string id);

    /// <summary>
    /// 获取角色的用户列表
    /// </summary>
    Task<List<UserDto>> GetRoleUsersAsync(string roleId);
    Task<List<OptionDto>> GetOptionsAsync();
} 
