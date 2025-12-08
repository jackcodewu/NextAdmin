using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NextAdmin.Application.DTOs.Captcha;
using NextAdmin.Redis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using NextAdmin.Application.Interfaces;

namespace NextAdmin.Application.Services
{
    /// <summary>
    /// Sliding puzzle captcha service implementation
    /// </summary>
    public class CaptchaService : ICaptchaService
    {
        private readonly IRedisService _redisService;
        private const int CaptchaWidth = 330; // Unified captcha background width
        private const int CaptchaHeight = 166; // Unified captcha background height
        private const int SliderWidth = 40; // Slider width
        private const int SliderHeight = 40; // Slider height
        private const int Tolerance = 3; // Allowed pixel tolerance
        private const int ExpireSeconds = 180; // Captcha validity period (seconds)

        public CaptchaService(IRedisService redisService)
        {
            _redisService = redisService;
        }

        /// <summary>
        /// Generate sliding puzzle captcha
        /// </summary>
        public async Task<CaptchaGenerateResultDto> GenerateCaptchaAsync()
        {
            // 1. Randomly select a local image as background
            var imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images");
            if (!Directory.Exists(imagesDir))
                throw new Exception($"Captcha image directory does not exist: {imagesDir}");
            var imageFiles = Directory.GetFiles(imagesDir, "*.jpg").Concat(Directory.GetFiles(imagesDir, "*.png")).ToArray();
            if (imageFiles.Length == 0)
                throw new Exception($"Captcha image directory is empty: {imagesDir}");
            var random = new Random();
            var imagePath = imageFiles[random.Next(imageFiles.Length)];

            using var srcImg = Image.Load<Rgba32>(imagePath);
            // Uniformly resize to 330x166
            srcImg.Mutate(x => x.Resize(CaptchaWidth, CaptchaHeight));

            int bgWidth = CaptchaWidth;
            int bgHeight = CaptchaHeight;
            int y = RandomNumberGenerator.GetInt32(20, bgHeight - SliderHeight - 20); // Gap target Y
            int x = RandomNumberGenerator.GetInt32(60, bgWidth - SliderWidth - 20);   // Gap target X
            int sliderStartX = RandomNumberGenerator.GetInt32(0, 31); // Slider initial X

            var rect = new Rectangle(x, y, SliderWidth, SliderHeight);
            using var bg = srcImg.Clone();
            bg.Mutate(ctx => ctx.Fill(Color.FromRgba(255,255,255,128), rect));
            using var slider = srcImg.Clone(ctx => ctx.Crop(rect));

            // 4. Convert to Base64
            string bgBase64, sliderBase64;
            using (var ms = new MemoryStream())
            {
                bg.SaveAsPng(ms);
                bgBase64 = Convert.ToBase64String(ms.ToArray());
            }
            using (var ms = new MemoryStream())
            {
                slider.SaveAsPng(ms);
                sliderBase64 = Convert.ToBase64String(ms.ToArray());
            }

            // 5. Generate Token and store in Redis (only store target x)
            var token = Guid.NewGuid().ToString("N");
            await _redisService.SetStringAsync($"captcha:{token}", x.ToString(), TimeSpan.FromSeconds(ExpireSeconds));

            return new CaptchaGenerateResultDto
            {
                Token = token,
                BackgroundImageBase64 = bgBase64,
                SliderImageBase64 = sliderBase64,
                Y = y,
                X = x,
                SliderStartX = sliderStartX, // New field
                SliderWidth = SliderWidth,
                SliderHeight = SliderHeight,
                Width = bgWidth,
                Height = bgHeight
            };
        }

        /// <summary>
        /// Verify sliding puzzle captcha
        /// </summary>
        public async Task<bool> VerifyCaptchaAsync(CaptchaVerifyDto dto, bool isDelete = false)
        {
            var redisKey = $"captcha:{dto.Token}";
            var xStr = await _redisService.GetStringAsync(redisKey);
            if (string.IsNullOrEmpty(xStr)) return false;

            if (!int.TryParse(xStr, out int correctX)) return false;

            if (isDelete)
                await _redisService.DeleteAsync(redisKey); // Prevent replay attack

            // Verify tolerance
            if (Math.Abs(dto.X - correctX) <= Tolerance)
            {
                // Optional: Verify trajectory validity
                return true;
            }
            return false;
        }
    }
} 
