using MongoDB.Bson;
using NextAdmin.Application.DTOs.Bases;

namespace NextAdmin.Application.DTOs;

/// <summary>
/// 用户DTO
/// </summary>
public class UserDto : BaseDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// 姓
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// 名
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 部门
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 用户角色
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

} 
