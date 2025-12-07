namespace NextAdmin.API.Models.Auth
{
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// 滑动验证码Token
        /// </summary>
        public string? CaptchaToken { get; set; }

        /// <summary>
        /// 滑块最终X坐标
        /// </summary>
        public int? CaptchaX { get; set; }

        /// <summary>
        /// 滑动轨迹（可选）
        /// </summary>
        public List<int>? CaptchaTrack { get; set; }
    }
}
