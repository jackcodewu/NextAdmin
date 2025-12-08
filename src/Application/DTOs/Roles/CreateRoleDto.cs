using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Core.Domain.Entities.Sys;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs;

/// <summary>
/// Create role DTO
/// </summary>
public class CreateRoleDto : CreateDto
{
    /// <summary>
    /// Role name
    /// </summary>
    [Required(ErrorMessage = "Role name cannot be empty")]
    [StringLength(50, ErrorMessage = "Role name length cannot exceed 50 characters")]
    public required string Name { get; set; }

    /// <summary>
    /// Role description
    /// </summary>
    [StringLength(200, ErrorMessage = "Role description length cannot exceed 200 characters")]
    public string? Description { get; set; }


    /// <summary>
    /// Tenant ID (optional)
    /// </summary>
    public string? TenantId { get; set; }
}
