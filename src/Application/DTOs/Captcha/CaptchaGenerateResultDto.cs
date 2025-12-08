using System;

namespace NextAdmin.Application.DTOs.Captcha
{
    /// <summary>
    /// Sliding puzzle captcha generation result DTO
    /// </summary>
    public class CaptchaGenerateResultDto
    {
        /// <summary>
        /// Unique captcha token for non-identity verification
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Background image Base64 string
        /// </summary>
        public string BackgroundImageBase64 { get; set; }

        /// <summary>
        /// Slider image Base64 string
        /// </summary>
        public string SliderImageBase64 { get; set; }

        /// <summary>
        /// Slider Y coordinate
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Slider X coordinate
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Slider width
        /// </summary>
        public int SliderWidth { get; set; }

        /// <summary>
        /// Slider height
        /// </summary>
        public int SliderHeight { get; set; }

        /// <summary>
        /// Image width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Image height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Slider initial X coordinate (frontend slider initial display position)
        /// </summary>
        public int SliderStartX { get; set; }
    }
} 
