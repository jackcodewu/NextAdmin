using System.Threading.Tasks;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Captcha;
using NextAdmin.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NextAdmin.API.Controllers
{
    /// <summary>
    /// 滑动拼图验证码控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CaptchaController : ControllerBase
    {
        private readonly ICaptchaService _captchaService;

        public CaptchaController(ICaptchaService captchaService)
        {
            _captchaService = captchaService;
        }

        /// <summary>
        /// 生成滑动拼图验证码
        /// </summary>
        [HttpGet("generate")]
        public async Task<ActionResult<CaptchaGenerateResultDto>> Generate()
        {
            var result = await _captchaService.GenerateCaptchaAsync();
            return Ok(ApiResponse<CaptchaGenerateResultDto>.SuccessResponse(result, "验证码生成成功"));
        }

        /// <summary>
        /// 校验滑动拼图验证码
        /// </summary>
        [HttpPost("verify")]
        public async Task<ActionResult> Verify([FromBody] CaptchaVerifyDto dto)
        {
            var isValid = await _captchaService.VerifyCaptchaAsync(dto);
            if (isValid)
                return Ok(ApiResponse<object>.SuccessResponse(new object(), "验证码校验成功"));
            return BadRequest(ApiResponse<object>.ErrorResponse("CAPTCHA_VERIFY_FAILED", "验证码校验失败"));
        }
    }
} 
