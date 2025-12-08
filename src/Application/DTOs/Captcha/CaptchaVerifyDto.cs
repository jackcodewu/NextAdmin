using System.Collections.Generic;

namespace NextAdmin.Application.DTOs.Captcha
{
    /// <summary>
    /// Sliding puzzle captcha verification DTO
    /// </summary>
    public class CaptchaVerifyDto
    {
        /// <summary>
        /// Captcha token for non-identity verification
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// User slider final X coordinate
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Sliding track (optional)
        /// </summary>
        public List<int> Track { get; set; }
    }
} 
