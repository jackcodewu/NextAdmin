namespace NextAdmin.API.Models.Auth
{
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Sliding captcha token
        /// </summary>
        public string? CaptchaToken { get; set; }

        /// <summary>
        /// Slider final X coordinate
        /// </summary>
        public int? CaptchaX { get; set; }

        /// <summary>
        /// Sliding track (optional)
        /// </summary>
        public List<int>? CaptchaTrack { get; set; }
    }
}
