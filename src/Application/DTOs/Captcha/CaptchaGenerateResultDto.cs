using System;

namespace NextAdmin.Application.DTOs.Captcha
{
    /// <summary>
    /// 滑动拼图验证码生成结果DTO
    /// </summary>
    public class CaptchaGenerateResultDto
    {
        /// <summary>
        /// 验证码唯一Token，用于校验拼图的非身份验证
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 背景图Base64字符串
        /// </summary>
        public string BackgroundImageBase64 { get; set; }

        /// <summary>
        /// 滑块图Base64字符串
        /// </summary>
        public string SliderImageBase64 { get; set; }

        /// <summary>
        /// 滑块Y坐标
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// 滑块X坐标
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// 滑块宽度
        /// </summary>
        public int SliderWidth { get; set; }

        /// <summary>
        /// 滑块高度
        /// </summary>
        public int SliderHeight { get; set; }

        /// <summary>
        /// 图片宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 图片高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 滑块初始X坐标（前端滑块初始显示位置）
        /// </summary>
        public int SliderStartX { get; set; }
    }
} 
