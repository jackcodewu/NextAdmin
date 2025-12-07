using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Core.Domain.Entities.Sys;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs;

/// <summary>
/// 创建角色DTO
/// </summary>
public class CreateRoleDto : CreateDto
{
    /// <summary>
    /// 角色名称
    /// </summary>
    [Required(ErrorMessage = "角色名称不能为空")]
    [StringLength(50, ErrorMessage = "角色名称长度不能超过50个字符")]
    public required string Name { get; set; }

    /// <summary>
    /// 角色描述
    /// </summary>
    [StringLength(200, ErrorMessage = "角色描述长度不能超过200个字符")]
    public string? Description { get; set; }


    /// <summary>
    /// 公司ID(可选)
    /// </summary>
    public string? TenantId { get; set; }
}
