using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.Constants
{
    public sealed class JwtSettings
    {
        public const string SectionName = "Jwt";

        [Required, MinLength(32)]
        public required string SecretKey { get; set; }

        [Required]
        public required string Issuer { get; set; }

        [Required]
        public required string Audience { get; set; }

        [Required]
        public required bool VerifyCaptcha { get; set; }

        [Range(1, 43200)] // 1分钟到30天
        public int ExpirationInMinutes { get; set; } = 1440; // 默认24小时
    }
} 
