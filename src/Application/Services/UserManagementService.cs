using AutoMapper;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Auths;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
// using NextAdmin.Application.DTOs.Companies; // 已移除 Tenant 相关 DTO
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
using System.ComponentModel.Design;

namespace NextAdmin.Application.Services;

/// <summary>
/// 用户管理服务实现
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    // private readonly ITenantRepository _TenantRepository; // 已移除 Tenant 相关功能

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        // ITenantRepository TenantRepository, // 已移除 Tenant 相关功能
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        // _TenantRepository = TenantRepository; // 已移除 Tenant 相关功能
    }

    public async Task<List<OptionDto>> GetOptionsAsync()
    {
        return _userManager.Users.Select(x => new OptionDto { value = x.Id.ToString(), label = x.UserName }).ToList();
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    public async Task<PagedResultDto<UsersDto>> GetUsersAsync(UserQueryDto userQueryDto, int pageNumber = 1, int pageSize = 10)
    {
        var users = _userManager.Users.AsQueryable();
        var expression= userQueryDto.ToExpression();
        users = users.Where(expression);

        var totalCount = users.Count();
        var userList = users.Skip((pageNumber - 1) * pageSize)
                           .Take(pageSize)
                           .ToList();

        //var userDtos = new List<UserDto>();
        //foreach (var user in userList)
        //{
        //    var roles = await _userManager.GetRolesAsync(user);
        //    userDtos.Add(MapToUserDto(user, roles));
        //}

        var userDtos = _mapper.Map<List<UsersDto>>(userList);

        return new PagedResultDto<UsersDto>(totalCount, userDtos);
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles);
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    public async Task<UserDto?> GetUserByUserNameAsync(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    public async Task<Result<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        // 检查用户名是否已存在
        var existingUser = await _userManager.FindByNameAsync(createUserDto.UserName);
        if (existingUser != null)
        {
            return Result<UserDto>.Failure("用户名已存在");
        }

        //// 检查邮箱是否已存在
        //existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
        //if (existingUser != null)
        //{
        //    return Result<UserDto>.Failure("邮箱已存在");
        //}

        // 已移除 Tenant 相关逻辑
        // var TenantId = GetTenantId();
        // var Tenant = await _TenantRepository.GetByIdAsync(TenantId);
        // if (Tenant == null)
        // {
        //     return Result<UserDto>.Failure("公司不存在");
        // }

        var user = new ApplicationUser
        {
            UserName = createUserDto.UserName,
            Email = ObjectId.GenerateNewId().ToString()+"@example.com",
            //FirstName = createUserDto.FirstName,
            //LastName = createUserDto.LastName,
            //DisplayName = createUserDto.DisplayName,
            //Department = createUserDto.Department,
            //Position = createUserDto.Position,
            //IsActive = createUserDto.IsActive,
            // TenantId = TenantId, // 已移除 Tenant 相关属性
            // TenantName = Tenant.Name, // 已移除 Tenant 相关属性
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, createUserDto.Password);
        if (!result.Succeeded)
        {
            return Result<UserDto>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // 分配角色
        if (createUserDto.RoleIds.Any())
        {
            var roles = new List<string>();
            foreach (var roleId in createUserDto.RoleIds)
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role != null)
                {
                    roles.Add(role.Name!);
                }
            }
            if (roles.Any())
            {
                await _userManager.AddToRolesAsync(user, roles);
            }
        }

        var userDto = MapToUserDto(user, await _userManager.GetRolesAsync(user));
        return Result<UserDto>.Success(userDto);
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    public async Task<Result<UserDto>> UpdateUserAsync(string id, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Result<UserDto>.Failure("用户不存在");
        }

        //// 检查邮箱是否被其他用户使用
        //var existingUser = await _userManager.FindByEmailAsync(updateUserDto.Email);
        //if (existingUser != null && existingUser.Id != user.Id)
        //{
        //    return Result<UserDto>.Failure("邮箱已被其他用户使用");
        //}

        //user.Email = updateUserDto.Email;
        //user.FirstName = updateUserDto.FirstName;
        //user.LastName = updateUserDto.LastName;
        //user.DisplayName = updateUserDto.DisplayName;
        //user.Department = updateUserDto.Department;
        //user.Position = updateUserDto.Position;
        //user.IsActive = updateUserDto.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Result<UserDto>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // 更新角色
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (updateUserDto.RoleIds.Any())
        {
            var roles = new List<string>();
            foreach (var roleId in updateUserDto.RoleIds)
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role != null)
                {
                    roles.Add(role.Name!);
                }
            }
            if (roles.Any())
            {
                await _userManager.AddToRolesAsync(user, roles);
            }
        }

        var userDto = MapToUserDto(user, await _userManager.GetRolesAsync(user));
        return Result<UserDto>.Success(userDto);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public async Task<Result> DeleteUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Result.Failure("用户不存在");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Result.Success();
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("用户不存在");
        }

        var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        if (!result.Succeeded)
        {
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Result.Success();
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    public async Task<Result<string>> ResetPasswordAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<string>.Failure("用户不存在");
        }

        var newPassword = GenerateRandomPassword();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            return Result<string>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Result<string>.Success(newPassword);
    }

    /// <summary>
    /// 激活/停用用户
    /// </summary>
    public async Task<Result> ToggleUserStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("用户不存在");
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Result.Success();
    }

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    public async Task<Result> AssignRolesToUserAsync(string userId, List<string> roleIds)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("用户不存在");
        }

        // 移除现有角色
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // 添加新角色
        if (roleIds.Any())
        {
            var roles = new List<string>();
            foreach (var roleId in roleIds)
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role != null)
                {
                    roles.Add(role.Name!);
                }
            }
            if (roles.Any())
            {
                var result = await _userManager.AddToRolesAsync(user, roles);
                if (!result.Succeeded)
                {
                    return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 获取用户的角色
    /// </summary>
    public async Task<List<RolesDto>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return new List<RolesDto>();

        var roleNames = await _userManager.GetRolesAsync(user);
        var roleDtos = new List<RolesDto>();

        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                roleDtos.Add(new RolesDto
                {
                    Id = role.Id.ToString(),
                    Name = role.Name!,
                    Description = role.Description,
                    UserCount = 0 // 这里可以根据需要计算用户数量
                });
            }
        }

        return roleDtos;
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

    public string GetTenantName(string? inputTenantId = null)
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("TenantName")?.Value ?? string.Empty;
    }

    // 已移除 Tenant 相关功能
    // public async Task<Result<TenantDto>> GetTenantAsync()
    // {
    //     ObjectId.TryParse(
    //        _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value,
    //        out ObjectId TenantId
    //    );
    //     var Tenant = await _TenantRepository.GetByIdAsync(TenantId);
    //     if (Tenant == null)
    //     {
    //         return Result<TenantDto>.Failure("无法查询到当前用户所属公司，请确认是否登录");
    //     }
    //     var TenantDto = _mapper.Map<TenantDto>(Tenant);
    //     TenantDto.Permissions.Clear();
    //     TenantDto.Menus.Clear();
    //     return Result<TenantDto>.Success(TenantDto);
    // }

    /// <summary>
    /// 映射到用户DTO
    /// </summary>
    private static UserDto MapToUserDto(ApplicationUser user, IList<string> roles)
    {
        return new UserDto
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
            Roles = roles.ToList(),
            LastLoginTime = user.LastLoginAt
        };
    }

    /// <summary>
    /// 生成随机密码
    /// </summary>
    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

} 
