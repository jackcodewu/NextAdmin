using NextAdmin.Application.DTOs.Bases;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs;

/// <summary>
/// Update user DTO
/// </summary>
public class UpdateUserDto
{

    /// <summary>
    /// Primary key ID
    /// </summary>
    [Required(ErrorMessage = "Primary key cannot be empty")]
    [StringLength(30, ErrorMessage = "Primary key length cannot exceed 30")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    [Required(ErrorMessage = "Username cannot be empty")]
    [StringLength(50, ErrorMessage = "Username length cannot exceed 50 characters")]
    public required string UserName { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    [Required(ErrorMessage = "Password cannot be empty")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password length must be between 6-100 characters")]
    public required string Password { get; set; }

    /// <summary>
    /// User role ID list
    /// </summary>
    public List<string> RoleIds { get; set; } = new();
} 
