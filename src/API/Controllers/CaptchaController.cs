using System.Threading.Tasks;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Captcha;
using NextAdmin.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NextAdmin.API.Controllers
{
    /// <summary>
    /// Sliding puzzle captcha controller
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
        /// Generate sliding puzzle captcha
        /// </summary>
        [HttpGet("generate")]
        public async Task<ActionResult<CaptchaGenerateResultDto>> Generate()
        {
            var result = await _captchaService.GenerateCaptchaAsync();
            return Ok(ApiResponse<CaptchaGenerateResultDto>.SuccessResponse(result, "Captcha generated successfully"));
        }

        /// <summary>
        /// Verify sliding puzzle captcha
        /// </summary>
        [HttpPost("verify")]
        public async Task<ActionResult> Verify([FromBody] CaptchaVerifyDto dto)
        {
            var isValid = await _captchaService.VerifyCaptchaAsync(dto);
            if (isValid)
                return Ok(ApiResponse<object>.SuccessResponse(new object(), "Captcha verification successful"));
            return BadRequest(ApiResponse<object>.ErrorResponse("CAPTCHA_VERIFY_FAILED", "Captcha verification failed"));
        }
    }
} 
