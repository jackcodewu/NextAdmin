using AutoMapper;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.DTOs.Roles;
using NextAdmin.Application.DTOs.Users;
using NextAdmin.Application.Interfaces;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using NextAdmin.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextAdmin.Application.Services;

/// <summary>
/// Role management service implementation
/// </summary>
public class RoleManagementService : IRoleManagementService
{
    // private readonly ITenantRepository _TenantRepository; // Tenant related functionality removed
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    protected ObjectId CurrentTenantId => GetTenantId();

    public RoleManagementService(
        RoleManager<ApplicationRole> roleManager,
        // ITenantRepository TenantRepository, // Tenant related functionality removed
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        IMapper mapper
    )
    {
        // _TenantRepository = TenantRepository; // Tenant related functionality removed
        _roleManager = roleManager;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
    }
    public ObjectId GetTenantId(string? inputTenantId = null)
    {

        // Super admin: Allow specifying TenantId, otherwise return Empty
        if (
            !string.IsNullOrEmpty(inputTenantId)
            && ObjectId.TryParse(inputTenantId, out var cid)
        )
            return cid;

        // Regular user: Always use their own
        ObjectId.TryParse(
            _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value,
            out ObjectId TenantId
        );

        if (TenantId == ObjectId.Empty)
            throw new Exception("Tenant is empty");

        return TenantId;
    }

    /// <summary>
    /// Create role
    /// </summary>
    public async Task<Result<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto)
    {
        // Check if role name already exists
        var existingRole = await _roleManager.FindByNameAsync(createRoleDto.Name);
        if (existingRole != null)
        {
            return Result<RoleDto>.Failure("Role name already exists");
        }

        var role = new ApplicationRole
        {
            Name = createRoleDto.Name,
            Description = createRoleDto.Description,
            CreatedAt = DateTime.UtcNow,
        };

        // Tenant related logic removed
        // var TenantId = GetTenantId(createRoleDto.TenantId);
        // var Tenant = await _TenantRepository.GetByIdAsync(TenantId);
        // if (Tenant == null)
        // {
        //     return Result<RoleDto>.Failure("Company does not exist");
        // }
        // role.TenantId = TenantId;
        // role.TenantName = Tenant.Name;
        // role.Menus = Tenant.Menus;
        // role.Permissions = Tenant.Permissions;

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return Result<RoleDto>.Failure(
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
        }

        var roleDto = MapToRoleDto(role, 0);
        return Result<RoleDto>.Success(roleDto);
    }

    /// <summary>
    /// Update role
    /// </summary>
    public async Task<Result<RoleDto>> UpdateRoleAsync(string id, UpdateRoleDto updateRoleDto)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return Result<RoleDto>.Failure("Role does not exist");
        }

        // Check if role name is used by other roles
        var existingRole = await _roleManager.FindByNameAsync(updateRoleDto.Name);
        if (existingRole != null && existingRole.Id != role.Id)
        {
            return Result<RoleDto>.Failure("Role name is already used by another role");
        }
        
        // Tenant related logic removed
        // var TenantId = GetTenantId(updateRoleDto.TenantId);
        // var Tenant = await _TenantRepository.GetByIdAsync(TenantId);
        // if (Tenant == null)
        // {
        //     return Result<RoleDto>.Failure("Company does not exist");
        // }
        // role.TenantId = TenantId;
        
        role.Name = updateRoleDto.Name;
        role.Description = updateRoleDto.Description;
        role.Menus = updateRoleDto.Menus;
        role.Permissions = updateRoleDto.Permissions;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return Result<RoleDto>.Failure(
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
        }

        var userCount = await GetRoleUserCountAsync(role.Name!);
        var roleDto = MapToRoleDto(role, userCount);
        return Result<RoleDto>.Success(roleDto);
    }

    public async Task<List<OptionDto>> GetOptionsAsync()
    {
        return _roleManager.Roles.Select(x => new OptionDto { value = x.Id.ToString(), label = x.Name }).ToList();
    }

    /// <summary>
    /// Get role list
    /// </summary>
    public async Task<PagedResultDto<RolesDto>> GetRolesAsync(
        RoleQueryDto roleQueryDto,
        int pageNumber = 1,
        int pageSize = 10
    )
    {
        var roles = _roleManager.Roles.AsQueryable();

        var expression = roleQueryDto.ToExpression();
        roles = roles.Where(expression);

        var totalCount = roles.Count();
        var roleList = roles.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        //var roleDtos = new List<RoleDto>();
        //foreach (var role in roleList)
        //{
        //    var userCount = await GetRoleUserCountAsync(role.Name!);
        //    roleDtos.Add(MapToRoleDto(role, userCount));
        //}

        var roleDtos = _mapper.Map<List<RolesDto>>(roleList);

        return new PagedResultDto<RolesDto>(totalCount, roleDtos);
    }

    /// <summary>
    /// Get all roles (without pagination)
    /// </summary>
    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        var roles = _roleManager.Roles.ToList();
        var roleDtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var userCount = await GetRoleUserCountAsync(role.Name!);
            roleDtos.Add(MapToRoleDto(role, userCount));
        }

        return roleDtos;
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    public async Task<RoleDto?> GetRoleByIdAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return null;

        var userCount = await GetRoleUserCountAsync(role.Name!);
        return MapToRoleDto(role, userCount);
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    public async Task<RoleDto?> GetRoleByNameAsync(string name)
    {
        var role = await _roleManager.FindByNameAsync(name);
        if (role == null)
            return null;

        var userCount = await GetRoleUserCountAsync(role.Name!);
        return MapToRoleDto(role, userCount);
    }

    /// <summary>
    /// Delete role
    /// </summary>
    public async Task<Result> DeleteRoleAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return Result.Failure("Role does not exist");
        }

        // Check if any users are using this role
        var userCount = await GetRoleUserCountAsync(role.Name!);
        if (userCount > 0)
        {
            return Result.Failure($"Cannot delete role, {userCount} user(s) are currently using this role");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Result.Success();
    }

    /// <summary>
    /// Get role's user list
    /// </summary>
    public async Task<List<UserDto>> GetRoleUsersAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            return new List<UserDto>();

        var users = await _userManager.GetUsersInRoleAsync(role.Name!);
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            userDtos.Add(
                new UserDto
                {
                    Id = user.Id.ToString(),
                    UserName = user.UserName!,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    DisplayName = user.DisplayName,
                    Department = user.Department,
                    Position = user.Position,
                    IsActive = user.IsActive,
                    Roles = userRoles.ToList(),
                    LastLoginTime = user.LastLoginAt,
                }
            );
        }

        return userDtos;
    }

    /// <summary>
    /// Get role's user count
    /// </summary>
    private async Task<int> GetRoleUserCountAsync(string roleName)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);
        return users.Count;
    }

    /// <summary>
    /// Map to role DTO
    /// </summary>
    private RoleDto MapToRoleDto(ApplicationRole role, int userCount)
    {
        var dto = _mapper.Map<RoleDto>(role);
        dto.UserCount = userCount;
        return dto;
    }
}
