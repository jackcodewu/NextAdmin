using System.Threading.Tasks;
using NextAdmin.Application.DTOs.Captcha;

namespace NextAdmin.Application.Interfaces
{
    /// <summary>
    /// Sliding puzzle captcha service interface
    /// </summary>
    public interface ICaptchaService
    {
        /// <summary>
        /// Generate sliding puzzle captcha
        /// </summary>
        Task<CaptchaGenerateResultDto> GenerateCaptchaAsync();

        /// <summary>
        /// Verify sliding puzzle captcha
        /// </summary>
        Task<bool> VerifyCaptchaAsync(CaptchaVerifyDto dto, bool isDelete = false);
    }
} 
