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
/// 用户管理控制器
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
    /// 获取用户列表
    /// </summary>
    [Authorize(Policy = PermissionsDefine.UserPermissions.View)]
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] UserQueryDto userQueryDto,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _userManagementService.GetUsersAsync(userQueryDto,pageNumber, pageSize);

        return Ok(ApiResponse<PagedResultDto<UsersDto>>.SuccessResponse(result, "查询成功"));
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    [Authorize(Policy = PermissionsDefine.UserPermissions.View)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _userManagementService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound("用户不存在");
        }

        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "操作成功"));
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    [Authorize(Policy = PermissionsDefine.UserPermissions.View)]
    [HttpGet("by-username/{userName}")]
    public async Task<IActionResult> GetUserByUserName(string userName)
    {
        var user = await _userManagementService.GetUserByUserNameAsync(userName);
        if (user == null)
        {
            return NotFound("用户不存在");
        }

        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "操作成功"));
    }

    /// <summary>
    /// 获取所有value/lable数据
    /// </summary>
    /// <returns></returns>
    [HttpGet("options")]
    [Authorize(Policy = PermissionsDefine.UserPermissions.View)]
    public virtual async Task<IActionResult> GetOptionsAsync()
    {
        var all = await _userManagementService.GetOptionsAsync();

        return Ok(ApiResponse<List<OptionDto>>.SuccessResponse(all, "查询成功"));
    }

    /// <summary>
    /// 创建用户
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
            return Ok(ApiResponse<UserDto>.SuccessResponse(result.Data, "操作成功"));
        }



        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// 更新用户
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

            return Ok(ApiResponse<bool>.SuccessResponse(true, "操作成功"));
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [Authorize(Policy = PermissionsDefine.UserPermissions.Delete)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var result = await _userManagementService.DeleteUserAsync(id);
        if (result.IsSuccess)
        {

            return Ok(ApiResponse<bool>.SuccessResponse(true, "操作成功"));
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// 修改密码
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

            return Ok(ApiResponse<bool>.SuccessResponse(true, "密码修改成功"));
        }


        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = PermissionsDefine.UserPermissions.Edit)]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var result = await _userManagementService.ResetPasswordAsync(id);
        if (result.IsSuccess)
        {

            return Ok(ApiResponse<object>.SuccessResponse(new { success = true, newPassword = result.Data }, "密码重置成功"));
        }


        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// 激活/停用用户
    /// </summary>
    [HttpPost("{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var result = await _userManagementService.ToggleUserStatusAsync(id);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<bool>.SuccessResponse(true, "操作成功"));
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    [HttpPost("{id}/assign-roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRolesToUser(string id, [FromBody] List<string> roleIds)
    {
        var result = await _userManagementService.AssignRolesToUserAsync(id, roleIds);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<bool>.SuccessResponse(true, "操作成功"));
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// 获取用户的角色
    /// </summary>
    [HttpGet("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserRoles(string id)
    {
        var roles = await _userManagementService.GetUserRolesAsync(id);

        return Ok(ApiResponse<List<RolesDto>>.SuccessResponse(roles, "操作成功"));
    }


}
 
