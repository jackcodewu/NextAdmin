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
            [Required(ErrorMessage = "Username cannot be empty")]
            [StringLength(50, ErrorMessage = "Username length cannot exceed 50 characters")]
            string UserName,
            
            [Required(ErrorMessage = "Password cannot be empty")]
            string Password,
            
            /// <summary>
            /// Sliding captcha token
            /// </summary>
            //[Required(ErrorMessage = "Captcha token cannot be empty")]
            string? CaptchaToken,
            
            /// <summary>
            /// Sliding captcha X coordinate
            /// </summary>
            //[Required(ErrorMessage = "Captcha X coordinate cannot be empty")]
            int? CaptchaX,
            
            /// <summary>
            /// Sliding captcha track
            /// </summary>
            //[Required(ErrorMessage = "Captcha track cannot be empty")]
            string? CaptchaTrack,

            /// <summary>
            /// Remember me
            /// </summary>
            bool RememberMe = false,

            /// <summary>
            /// Is mobile
            /// </summary>
            bool IsMobile = true
        );

        public sealed record RegisterRequest(
            [Required(ErrorMessage = "Username cannot be empty")]
            [StringLength(50, ErrorMessage = "Username length cannot exceed 50 characters")]
            string UserName,
            
            [Required(ErrorMessage = "Email cannot be empty")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            string Email,
            
            [Required(ErrorMessage = "Password cannot be empty")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password length must be between 6-100 characters")]
            string Password,
            
            [Required(ErrorMessage = "Confirm password cannot be empty")]
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
                    yield return new ValidationResult("Password and confirm password do not match", [nameof(ConfirmPassword)]);
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
            [Required(ErrorMessage = "Current password cannot be empty")]
            string CurrentPassword,
            
            [Required(ErrorMessage = "New password cannot be empty")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "New password length must be between 6-100 characters")]
            string NewPassword
        );

        public sealed record RefreshTokenRequest(
            [Required(ErrorMessage = "Token cannot be empty")]
            string Token
        );

        /// <summary>
        /// Member login request
        /// </summary>
        public sealed record MemberLoginRequest(
            [Required(ErrorMessage = "Username/Email/Phone cannot be empty")]
            [StringLength(100, ErrorMessage = "Length cannot exceed 100 characters")]
            string Account,
            
            [Required(ErrorMessage = "Password cannot be empty")]
            string Password,
            
            /// <summary>
            /// Remember me
            /// </summary>
            bool RememberMe = false
        );

        /// <summary>
        /// Member registration request
        /// </summary>
        public sealed record MemberRegisterRequest(
            [Required(ErrorMessage = "Username cannot be empty")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Username length must be between 2-50 characters")]
            string Username,
            
            [Required(ErrorMessage = "Nickname cannot be empty")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Nickname length must be between 2-50 characters")]
            string Nickname,
            
            [Required(ErrorMessage = "Password cannot be empty")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password length must be between 6-100 characters")]
            string Password,
            
            [Required(ErrorMessage = "Confirm password cannot be empty")]
            string ConfirmPassword,
            
            [EmailAddress(ErrorMessage = "Invalid email format")]
            string? Email = null,
            
            [Phone(ErrorMessage = "Invalid phone number format")]
            string? Phone = null,
            
            string? CurrentGrade = null,
            
            int DailyGoal = 20
        ) : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (Password != ConfirmPassword)
                {
                    yield return new ValidationResult("Password and confirm password do not match", [nameof(ConfirmPassword)]);
                }
            }
        };

        /// <summary>
        /// Member information
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
        /// Member authentication response (includes member information)
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
