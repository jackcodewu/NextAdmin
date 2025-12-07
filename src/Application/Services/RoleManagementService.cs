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
/// 角色管理服务实现
/// </summary>
public class RoleManagementService : IRoleManagementService
{
    // private readonly ITenantRepository _TenantRepository; // 已移除 Tenant 相关功能
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    protected ObjectId CurrentTenantId => GetTenantId();

    public RoleManagementService(
        RoleManager<ApplicationRole> roleManager,
        // ITenantRepository TenantRepository, // 已移除 Tenant 相关功能
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        IMapper mapper
    )
    {
        // _TenantRepository = TenantRepository; // 已移除 Tenant 相关功能
        _roleManager = roleManager;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
    }
    public ObjectId GetTenantId(string? inputTenantId = null)
    {

        // 超级管理员：允许指定TenantId，否则返回Empty
        if (
            !string.IsNullOrEmpty(inputTenantId)
            && ObjectId.TryParse(inputTenantId, out var cid)
        )
            return cid;

        // 普通用户：始终用自己
        ObjectId.TryParse(
            _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value,
            out ObjectId TenantId
        );

        if (TenantId == ObjectId.Empty)
            throw new Exception("Tenant is empty");

        return TenantId;
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    public async Task<Result<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto)
    {
        // 检查角色名是否已存在
        var existingRole = await _roleManager.FindByNameAsync(createRoleDto.Name);
        if (existingRole != null)
        {
            return Result<RoleDto>.Failure("角色名已存在");
        }

        var role = new ApplicationRole
        {
            Name = createRoleDto.Name,
            Description = createRoleDto.Description,
            CreatedAt = DateTime.UtcNow,
        };

        // 已移除 Tenant 相关逻辑
        // var TenantId = GetTenantId(createRoleDto.TenantId);
        // var Tenant = await _TenantRepository.GetByIdAsync(TenantId);
        // if (Tenant == null)
        // {
        //     return Result<RoleDto>.Failure("公司不存在");
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
    /// 更新角色
    /// </summary>
    public async Task<Result<RoleDto>> UpdateRoleAsync(string id, UpdateRoleDto updateRoleDto)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return Result<RoleDto>.Failure("角色不存在");
        }

        // 检查角色名是否被其他角色使用
        var existingRole = await _roleManager.FindByNameAsync(updateRoleDto.Name);
        if (existingRole != null && existingRole.Id != role.Id)
        {
            return Result<RoleDto>.Failure("角色名已被其他角色使用");
        }
        
        // 已移除 Tenant 相关逻辑
        // var TenantId = GetTenantId(updateRoleDto.TenantId);
        // var Tenant = await _TenantRepository.GetByIdAsync(TenantId);
        // if (Tenant == null)
        // {
        //     return Result<RoleDto>.Failure("公司不存在");
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
    /// 获取角色列表
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
    /// 获取所有角色（不分页）
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
    /// 根据ID获取角色
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
    /// 根据名称获取角色
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
    /// 删除角色
    /// </summary>
    public async Task<Result> DeleteRoleAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return Result.Failure("角色不存在");
        }

        // 检查是否有用户使用此角色
        var userCount = await GetRoleUserCountAsync(role.Name!);
        if (userCount > 0)
        {
            return Result.Failure($"无法删除角色，还有 {userCount} 个用户正在使用此角色");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Result.Success();
    }

    /// <summary>
    /// 获取角色的用户列表
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
    /// 获取角色的用户数量
    /// </summary>
    private async Task<int> GetRoleUserCountAsync(string roleName)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);
        return users.Count;
    }

    /// <summary>
    /// 映射到角色DTO
    /// </summary>
    private RoleDto MapToRoleDto(ApplicationRole role, int userCount)
    {
        var dto = _mapper.Map<RoleDto>(role);
        dto.UserCount = userCount;
        return dto;
    }
}
