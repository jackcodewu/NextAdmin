using NextAdmin.Application.Constants;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Auths;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.DTOs.Roles;
using NextAdmin.Application.DTOs.Users;
using NextAdmin.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NextAdmin.API.Controllers;

/// <summary>
/// User management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UserController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserController(
        IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>
    /// Get users list
    /// </summary>
    [Authorize(Policy = PermissionsDefine.UserPermissions.View)]
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] UserQueryDto userQueryDto,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _userManagementService.GetUsersAsync(userQueryDto,pageNumber, pageSize);

        return Ok(ApiResponse<PagedResultDto<UsersDto>>.SuccessResponse(result, "Query successful"));
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [Authorize(Policy = PermissionsDefine.UserPermissions.View)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _userManagementService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound("User does not exist");
        }

        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "Operation successful"));
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    [Authorize(Policy = PermissionsDefine.UserPermissions.View)]
    [HttpGet("by-username/{userName}")]
    public async Task<IActionResult> GetUserByUserName(string userName)
    {
        var user = await _userManagementService.GetUserByUserNameAsync(userName);
        if (user == null)
        {
            return NotFound("User does not exist");
        }

        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "Operation successful"));
    }

    /// <summary>
    /// Get all value/label data
    /// </summary>
    /// <returns></returns>
    [HttpGet("options")]
    [Authorize(Policy = PermissionsDefine.UserPermissions.View)]
    public virtual async Task<IActionResult> GetOptionsAsync()
    {
        var all = await _userManagementService.GetOptionsAsync();

        return Ok(ApiResponse<List<OptionDto>>.SuccessResponse(all, "Query successful"));
    }

    /// <summary>
    /// Create user
    /// </summary>
    [Authorize(Policy = PermissionsDefine.UserPermissions.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userManagementService.CreateUserAsync(createUserDto);
        if (result.IsSuccess)
        {    
            return Ok(ApiResponse<UserDto>.SuccessResponse(result.Data, "Operation successful"));
        }



        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Update user
    /// </summary>
    [Authorize(Policy = PermissionsDefine.UserPermissions.Edit)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto updateUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userManagementService.UpdateUserAsync(id, updateUserDto);
        if (result.IsSuccess)
        {

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Operation successful"));
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [Authorize(Policy = PermissionsDefine.UserPermissions.Delete)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var result = await _userManagementService.DeleteUserAsync(id);
        if (result.IsSuccess)
        {

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Operation successful"));
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPost("{id}/change-password")]
    [Authorize(Policy = PermissionsDefine.UserPermissions.Edit)]
    public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordDto changePasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userManagementService.ChangePasswordAsync(id, changePasswordDto);
        if (result.IsSuccess)
        {

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Password changed successfully"));
        }


        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Reset password
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = PermissionsDefine.UserPermissions.Edit)]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var result = await _userManagementService.ResetPasswordAsync(id);
        if (result.IsSuccess)
        {

            return Ok(ApiResponse<object>.SuccessResponse(new { success = true, newPassword = result.Data }, "Password reset successfully"));
        }


        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Activate/deactivate user
    /// </summary>
    [HttpPost("{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var result = await _userManagementService.ToggleUserStatusAsync(id);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Operation successful"));
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Assign roles to user
    /// </summary>
    [HttpPost("{id}/assign-roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRolesToUser(string id, [FromBody] List<string> roleIds)
    {
        var result = await _userManagementService.AssignRolesToUserAsync(id, roleIds);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Operation successful"));
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// Get user's roles
    /// </summary>
    [HttpGet("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserRoles(string id)
    {
        var roles = await _userManagementService.GetUserRolesAsync(id);

        return Ok(ApiResponse<List<RolesDto>>.SuccessResponse(roles, "Operation successful"));
    }


}
 
