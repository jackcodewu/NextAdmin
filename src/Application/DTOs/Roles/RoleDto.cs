using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Core.Domain.Entities.Sys;
using MongoDB.Bson;
using System.Collections.Generic;

namespace NextAdmin.Application.DTOs;

/// <summary>
/// 角色DTO
/// </summary>
public class RoleDto : BaseDto
{

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 用户数量
    /// </summary>
    public int UserCount { get; set; }
    

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 菜单列表
    /// </summary>
    public List<Menu> Menus { get; set; }

    /// <summary>
    /// 权限列表
    /// </summary>
    public List<Permission> Permissions { get; set; }
} 
