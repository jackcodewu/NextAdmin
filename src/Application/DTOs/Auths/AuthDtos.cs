using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs
{
    public class AuthDtos
    {
        public sealed record LoginRequest(
            [Required(ErrorMessage = "用户名不能为空")]
            [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
            string UserName,
            
            [Required(ErrorMessage = "密码不能为空")]
            string Password,
            
            /// <summary>
            /// 滑动验证码Token
            /// </summary>
            //[Required(ErrorMessage = "验证码Token不能为空")]
            string? CaptchaToken,
            
            /// <summary>
            /// 滑动验证码X坐标
            /// </summary>
            //[Required(ErrorMessage = "验证码X坐标不能为空")]
            int? CaptchaX,
            
            /// <summary>
            /// 滑动验证码轨迹
            /// </summary>
            //[Required(ErrorMessage = "验证码轨迹不能为空")]
            string? CaptchaTrack,

            /// <summary>
            /// 是否记住我
            /// </summary>
            bool RememberMe = false,

            /// <summary>
            /// 是否是移动端
            /// </summary>
            bool IsMobile = true
        );

        public sealed record RegisterRequest(
            [Required(ErrorMessage = "用户名不能为空")]
            [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
            string UserName,
            
            [Required(ErrorMessage = "邮箱不能为空")]
            [EmailAddress(ErrorMessage = "邮箱格式不正确")]
            string Email,
            
            [Required(ErrorMessage = "密码不能为空")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
            string Password,
            
            [Required(ErrorMessage = "确认密码不能为空")]
            string ConfirmPassword,
            
            string? FirstName = null,
            string? LastName = null,
            string? Department = null,
            string? Position = null
        ) : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (Password != ConfirmPassword)
                {
                    yield return new ValidationResult("密码和确认密码不匹配", [nameof(ConfirmPassword)]);
                }
            }
        }

        public sealed record AuthResponse(
            bool Success,
            string Message,
            string? Token = null,
            DateTime? ExpiresAt = null,
            UserInfo? User = null
        );

        public sealed record UserInfo(
            string Id,
            string UserName,
            string Email,
            string? FirstName,
            string? LastName,
            string? DisplayName,
            string? Department,
            string? Position,
            string ? TenantId,
            List<string> Roles,
            DateTime CreatedAt,
            DateTime? LastLoginAt
        );

        public sealed record ChangePasswordRequest(
            [Required(ErrorMessage = "当前密码不能为空")]
            string CurrentPassword,
            
            [Required(ErrorMessage = "新密码不能为空")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "新密码长度必须在6-100个字符之间")]
            string NewPassword
        );

        public sealed record RefreshTokenRequest(
            [Required(ErrorMessage = "令牌不能为空")]
            string Token
        );

        /// <summary>
        /// 会员登录请求
        /// </summary>
        public sealed record MemberLoginRequest(
            [Required(ErrorMessage = "用户名/邮箱/手机号不能为空")]
            [StringLength(100, ErrorMessage = "长度不能超过100个字符")]
            string Account,
            
            [Required(ErrorMessage = "密码不能为空")]
            string Password,
            
            /// <summary>
            /// 是否记住我
            /// </summary>
            bool RememberMe = false
        );

        /// <summary>
        /// 会员注册请求
        /// </summary>
        public sealed record MemberRegisterRequest(
            [Required(ErrorMessage = "用户名不能为空")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "用户名长度必须在2-50个字符之间")]
            string Username,
            
            [Required(ErrorMessage = "昵称不能为空")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "昵称长度必须在2-50个字符之间")]
            string Nickname,
            
            [Required(ErrorMessage = "密码不能为空")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
            string Password,
            
            [Required(ErrorMessage = "确认密码不能为空")]
            string ConfirmPassword,
            
            [EmailAddress(ErrorMessage = "邮箱格式不正确")]
            string? Email = null,
            
            [Phone(ErrorMessage = "手机号格式不正确")]
            string? Phone = null,
            
            string? CurrentGrade = null,
            
            int DailyGoal = 20
        ) : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (Password != ConfirmPassword)
                {
                    yield return new ValidationResult("密码和确认密码不匹配", [nameof(ConfirmPassword)]);
                }
            }
        };

        /// <summary>
        /// 会员信息
        /// </summary>
        public sealed record MemberInfo(
            string Id,
            string UserId,
            string Username,
            string Nickname,
            string? Avatar,
            string? Email,
            string? Phone,
            string MemberType,
            DateTime? MemberExpireTime,
            int TotalStudyDays,
            int ContinuousStudyDays,
            int TotalWordsLearned,
            int TodayWordsLearned,
            int DailyGoal,
            DateTime CreateTime
        );

        /// <summary>  
        /// 会员认证响应（包含会员信息）
        /// </summary>
        public sealed record MemberAuthResponse(
            bool Success,
            string Message,
            string? Token = null,
            DateTime? ExpiresAt = null,
            UserInfo? User = null,
            MemberInfo? Member = null
        );
    }
}
