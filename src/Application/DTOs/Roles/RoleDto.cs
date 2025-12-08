using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Core.Domain.Entities.Sys;
using MongoDB.Bson;
using System.Collections.Generic;

namespace NextAdmin.Application.DTOs;

/// <summary>
/// Role DTO
/// </summary>
public class RoleDto : BaseDto
{

    /// <summary>
    /// Role description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// User count
    /// </summary>
    public int UserCount { get; set; }
    

    /// <summary>
    /// Create time
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Menu list
    /// </summary>
    public List<Menu> Menus { get; set; }

    /// <summary>
    /// Permission list
    /// </summary>
    public List<Permission> Permissions { get; set; }
} 
