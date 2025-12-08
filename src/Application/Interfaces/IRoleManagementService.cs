using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.DTOs.Roles;
using NextAdmin.Application.DTOs.Users;
using NextAdmin.Shared.Common;

namespace NextAdmin.Application.Interfaces;

/// <summary>
/// Role management service interface
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// Get role list
    /// </summary>
    Task<PagedResultDto<RolesDto>> GetRolesAsync(RoleQueryDto roleQueryDto,int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// Get all roles (without pagination)
    /// </summary>
    Task<List<RoleDto>> GetAllRolesAsync();

    /// <summary>
    /// Get role by ID
    /// </summary>
    Task<RoleDto?> GetRoleByIdAsync(string id);

    /// <summary>
    /// Get role by name
    /// </summary>
    Task<RoleDto?> GetRoleByNameAsync(string name);

    /// <summary>
    /// Create role
    /// </summary>
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto);

    /// <summary>
    /// Update role
    /// </summary>
    Task<Result<RoleDto>> UpdateRoleAsync(string id, UpdateRoleDto updateRoleDto);

    /// <summary>
    /// Delete role
    /// </summary>
    Task<Result> DeleteRoleAsync(string id);

    /// <summary>
    /// Get role's user list
    /// </summary>
    Task<List<UserDto>> GetRoleUsersAsync(string roleId);
    Task<List<OptionDto>> GetOptionsAsync();
} 
