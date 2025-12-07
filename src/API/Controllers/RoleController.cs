using NextAdmin.API.Models;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.DTOs.Roles;
using NextAdmin.Application.Interfaces;
using NextAdmin.Application.Services;
using NextAdmin.Shared.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static NextAdmin.Application.Constants.PermissionsDefine;

namespace NextAdmin.API.Controllers;

/// <summary>
/// 角色管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RoleController : ControllerBase
{
    private readonly IRoleManagementService _roleManagementService;

    public RoleController(IRoleManagementService roleManagementService)
    {
        _roleManagementService = roleManagementService;
    }

    /// <summary>
    /// 获取角色列表
    /// </summary>
    [HttpGet]
    [Authorize(Policy = RolePermissions.View)]
    public async Task<IActionResult> GetRoles(
        [FromQuery] RoleQueryDto roleQueryDto,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _roleManagementService.GetRolesAsync(roleQueryDto,pageNumber, pageSize);
        return Ok(ApiResponse<PagedResultDto<RolesDto>>.SuccessResponse(result, "查询成功"));
    }

    /// <summary>
    /// 获取所有角色（不分页）
    /// </summary>
    [HttpGet("all")]
    [Authorize(Policy = RolePermissions.View)]
    private async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleManagementService.GetAllRolesAsync();
        return Ok(ApiResponse<List<RoleDto>>.SuccessResponse(roles, "查询成功"));
    }
    
    /// <summary>
     /// 获取所有value/lable数据
     /// </summary>
     /// <returns></returns>
    [HttpGet("options")]
    [Authorize(Policy = RolePermissions.View)]
    public virtual async Task<IActionResult> GetOptionsAsync()
    {
        var all = await _roleManagementService.GetOptionsAsync();

        return Ok(ApiResponse<List<OptionDto>>.SuccessResponse(all, "查询成功"));
    }

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = RolePermissions.View)]
    public async Task<IActionResult> GetRole(string id)
    {
        var role = await _roleManagementService.GetRoleByIdAsync(id);
        if (role == null)
        {
            return NotFound("角色不存在");
        }

        return Ok(ApiResponse<RoleDto>.SuccessResponse(role, "查询成功"));
    }

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    [HttpGet("by-name/{name}")]
    [Authorize(Policy = RolePermissions.View)]
    public async Task<IActionResult> GetRoleByName(string name)
    {
        var role = await _roleManagementService.GetRoleByNameAsync(name);
        if (role == null)
        {
            return NotFound("角色不存在");
        }

        return Ok(ApiResponse<RoleDto>.SuccessResponse(role, "查询成功"));
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    [HttpPost]
    [Authorize(Policy = RolePermissions.Create)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto createRoleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _roleManagementService.CreateRoleAsync(createRoleDto);
        if (result.IsSuccess)
        {

            return Ok(ApiResponse<RoleDto>.SuccessResponse(result.Data, "查询成功"));
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = RolePermissions.Edit)]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateRoleDto updateRoleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _roleManagementService.UpdateRoleAsync(id, updateRoleDto);
        if (result.IsSuccess)
        {

            return Ok(ApiResponse<bool>.SuccessResponse(true, "查询成功"));
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = RolePermissions.Delete)]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var result = await _roleManagementService.DeleteRoleAsync(id);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return BadRequest(result.ErrorMessage);
    }

    /// <summary>
    /// 获取角色的用户列表
    /// </summary>
    [HttpGet("{id}/users")]
    [Authorize(Policy = RolePermissions.View)]
    public async Task<IActionResult> GetRoleUsers(string id)
    {
        var users = await _roleManagementService.GetRoleUsersAsync(id);

        return Ok(ApiResponse<List<UserDto>>.SuccessResponse(users, "查询成功"));
    }
} 
