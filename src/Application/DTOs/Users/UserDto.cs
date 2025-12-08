using MongoDB.Bson;
using NextAdmin.Application.DTOs.Bases;

namespace NextAdmin.Application.DTOs;

/// <summary>
/// User DTO
/// </summary>
public class UserDto : BaseDto
{
    /// <summary>
    /// Username
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Department
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Position
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Last login time
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

} 
