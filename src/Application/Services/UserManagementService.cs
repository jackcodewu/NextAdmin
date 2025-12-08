using AutoMapper;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Auths;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
// using NextAdmin.Application.DTOs.Companies; // Tenant related DTO removed
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
/// User management service implementation
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    // private readonly ITenantRepository _TenantRepository; // Tenant related functionality removed

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        // ITenantRepository TenantRepository, // Tenant related functionality removed
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        // _TenantRepository = TenantRepository; // Tenant related functionality removed
    }

    public async Task<List<OptionDto>> GetOptionsAsync()
    {
        return _userManager.Users.Select(x => new OptionDto { value = x.Id.ToString(), label = x.UserName }).ToList();
    }

    /// <summary>
    /// Get user list
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
    /// Get user by ID
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles);
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    public async Task<UserDto?> GetUserByUserNameAsync(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles);
    }

    /// <summary>
    /// Create user
    /// </summary>
    public async Task<Result<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        // Check if username already exists
        var existingUser = await _userManager.FindByNameAsync(createUserDto.UserName);
        if (existingUser != null)
        {
            return Result<UserDto>.Failure("Username already exists");
        }

        //// Check if email already exists
        //existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
        //if (existingUser != null)
        //{
        //    return Result<UserDto>.Failure("Email already exists");
        //}

        // Tenant related logic removed
        // var TenantId = GetTenantId();
        // var Tenant = await _TenantRepository.GetByIdAsync(TenantId);
        // if (Tenant == null)
        // {
        //     return Result<UserDto>.Failure("Company does not exist");
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
            // TenantId = TenantId, // Tenant related property removed
            // TenantName = Tenant.Name, // Tenant related property removed
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, createUserDto.Password);
        if (!result.Succeeded)
        {
            return Result<UserDto>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Assign roles
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
    /// Update user
    /// </summary>
    public async Task<Result<UserDto>> UpdateUserAsync(string id, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Result<UserDto>.Failure("User does not exist");
        }

        //// Check if email is used by other users
        //var existingUser = await _userManager.FindByEmailAsync(updateUserDto.Email);
        //if (existingUser != null && existingUser.Id != user.Id)
        //{
        //    return Result<UserDto>.Failure("Email is already used by another user");
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

        // Update roles
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
    /// Delete user
    /// </summary>
    public async Task<Result> DeleteUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Result.Failure("User does not exist");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Result.Success();
    }

    /// <summary>
    /// Change password
    /// </summary>
    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User does not exist");
        }

        var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        if (!result.Succeeded)
        {
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Result.Success();
    }

    /// <summary>
    /// Reset password
    /// </summary>
    public async Task<Result<string>> ResetPasswordAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<string>.Failure("User does not exist");
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
    /// Activate/Deactivate user
    /// </summary>
    public async Task<Result> ToggleUserStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User does not exist");
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
    /// Assign roles to user
    /// </summary>
    public async Task<Result> AssignRolesToUserAsync(string userId, List<string> roleIds)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User does not exist");
        }

        // Remove current roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Add new roles
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
    /// Get user's roles
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
                    UserCount = 0 // User count can be calculated here if needed
                });
            }
        }

        return roleDtos;
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

    public string GetTenantName(string? inputTenantId = null)
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("TenantName")?.Value ?? string.Empty;
    }

    // Tenant related functionality removed
    // public async Task<Result<TenantDto>> GetTenantAsync()
    // {
    //     ObjectId.TryParse(
    //        _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value,
    //        out ObjectId TenantId
    //    );
    //     var Tenant = await _TenantRepository.GetByIdAsync(TenantId);
    //     if (Tenant == null)
    //     {
    //         return Result<TenantDto>.Failure("Unable to query the current user's company, please confirm if logged in");
    //     }
    //     var TenantDto = _mapper.Map<TenantDto>(Tenant);
    //     TenantDto.Permissions.Clear();
    //     TenantDto.Menus.Clear();
    //     return Result<TenantDto>.Success(TenantDto);
    // }

    /// <summary>
    /// Map to user DTO
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
    /// Generate random password
    /// </summary>
    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

} 
