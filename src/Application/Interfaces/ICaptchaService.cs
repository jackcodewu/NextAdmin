using System.Threading.Tasks;
using NextAdmin.Application.DTOs.Captcha;

namespace NextAdmin.Application.Interfaces
{
    /// <summary>
    /// 滑动拼图验证码服务接口
    /// </summary>
    public interface ICaptchaService
    {
        /// <summary>
        /// 生成滑动拼图验证码
        /// </summary>
        Task<CaptchaGenerateResultDto> GenerateCaptchaAsync();

        /// <summary>
        /// 校验滑动拼图验证码
        /// </summary>
        Task<bool> VerifyCaptchaAsync(CaptchaVerifyDto dto, bool isDelete = false);
    }
} 
