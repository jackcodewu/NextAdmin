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
    /// 滑动拼图验证码服务实现
    /// </summary>
    public class CaptchaService : ICaptchaService
    {
        private readonly IRedisService _redisService;
        private const int CaptchaWidth = 330; // 统一验证码背景宽度
        private const int CaptchaHeight = 166; // 统一验证码背景高度
        private const int SliderWidth = 40; // 滑块宽度
        private const int SliderHeight = 40; // 滑块高度
        private const int Tolerance = 3; // 允许误差像素
        private const int ExpireSeconds = 180; // 验证码有效期（秒）

        public CaptchaService(IRedisService redisService)
        {
            _redisService = redisService;
        }

        /// <summary>
        /// 生成滑动拼图验证码
        /// </summary>
        public async Task<CaptchaGenerateResultDto> GenerateCaptchaAsync()
        {
            // 1. 随机选取一张本地图片作为背景
            var imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images");
            if (!Directory.Exists(imagesDir))
                throw new Exception($"验证码图片目录不存在: {imagesDir}");
            var imageFiles = Directory.GetFiles(imagesDir, "*.jpg").Concat(Directory.GetFiles(imagesDir, "*.png")).ToArray();
            if (imageFiles.Length == 0)
                throw new Exception($"验证码图片目录为空: {imagesDir}");
            var random = new Random();
            var imagePath = imageFiles[random.Next(imageFiles.Length)];

            using var srcImg = Image.Load<Rgba32>(imagePath);
            // 统一缩放为330x166
            srcImg.Mutate(x => x.Resize(CaptchaWidth, CaptchaHeight));

            int bgWidth = CaptchaWidth;
            int bgHeight = CaptchaHeight;
            int y = RandomNumberGenerator.GetInt32(20, bgHeight - SliderHeight - 20); // 缺口目标Y
            int x = RandomNumberGenerator.GetInt32(60, bgWidth - SliderWidth - 20);   // 缺口目标X
            int sliderStartX = RandomNumberGenerator.GetInt32(0, 31); // 滑块初始X

            var rect = new Rectangle(x, y, SliderWidth, SliderHeight);
            using var bg = srcImg.Clone();
            bg.Mutate(ctx => ctx.Fill(Color.FromRgba(255,255,255,128), rect));
            using var slider = srcImg.Clone(ctx => ctx.Crop(rect));

            // 4. 转Base64
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

            // 5. 生成Token，存入Redis（只存目标x）
            var token = Guid.NewGuid().ToString("N");
            await _redisService.SetStringAsync($"captcha:{token}", x.ToString(), TimeSpan.FromSeconds(ExpireSeconds));

            return new CaptchaGenerateResultDto
            {
                Token = token,
                BackgroundImageBase64 = bgBase64,
                SliderImageBase64 = sliderBase64,
                Y = y,
                X = x,
                SliderStartX = sliderStartX, // 新增字段
                SliderWidth = SliderWidth,
                SliderHeight = SliderHeight,
                Width = bgWidth,
                Height = bgHeight
            };
        }

        /// <summary>
        /// 校验滑动拼图验证码
        /// </summary>
        public async Task<bool> VerifyCaptchaAsync(CaptchaVerifyDto dto, bool isDelete = false)
        {
            var redisKey = $"captcha:{dto.Token}";
            var xStr = await _redisService.GetStringAsync(redisKey);
            if (string.IsNullOrEmpty(xStr)) return false;

            if (!int.TryParse(xStr, out int correctX)) return false;

            if (isDelete)
                await _redisService.DeleteAsync(redisKey); // 防重放

            // 校验误差
            if (Math.Abs(dto.X - correctX) <= Tolerance)
            {
                // 可选：校验轨迹合理性
                return true;
            }
            return false;
        }
    }
} 
