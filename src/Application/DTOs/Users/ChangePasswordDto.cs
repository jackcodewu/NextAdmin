using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs.Auths;

/// <summary>
/// Change password DTO
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// Current password
    /// </summary>
    [Required(ErrorMessage = "Current password cannot be empty")]
    public required string CurrentPassword { get; set; }

    /// <summary>
    /// New password
    /// </summary>
    [Required(ErrorMessage = "New password cannot be empty")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "New password length must be between 6-100 characters")]
    public required string NewPassword { get; set; }

    /// <summary>
    /// Confirm new password
    /// </summary>
    [Required(ErrorMessage = "Confirm new password cannot be empty")]
    [Compare("NewPassword", ErrorMessage = "New password and confirm new password do not match")]
    public required string ConfirmNewPassword { get; set; }
} 
