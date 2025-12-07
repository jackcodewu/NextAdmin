using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs.Auths;

/// <summary>
/// 修改密码DTO
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// 当前密码
    /// </summary>
    [Required(ErrorMessage = "当前密码不能为空")]
    public required string CurrentPassword { get; set; }

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "新密码长度必须在6-100个字符之间")]
    public required string NewPassword { get; set; }

    /// <summary>
    /// 确认新密码
    /// </summary>
    [Required(ErrorMessage = "确认新密码不能为空")]
    [Compare("NewPassword", ErrorMessage = "新密码和确认新密码不匹配")]
    public required string ConfirmNewPassword { get; set; }
} 
