using System.Collections.Generic;

namespace NextAdmin.Application.DTOs.Captcha
{
    /// <summary>
    /// 滑动拼图验证码校验DTO
    /// </summary>
    public class CaptchaVerifyDto
    {
        /// <summary>
        /// 验证码Token，用于校验拼图的非身份验证
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 用户滑块最终X坐标
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// 滑动轨迹（可选）
        /// </summary>
        public List<int> Track { get; set; }
    }
} 
