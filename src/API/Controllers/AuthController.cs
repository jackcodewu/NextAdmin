using NextAdmin.Application.DTOs;
using NextAdmin.Application.Interfaces;
using NextAdmin.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using static NextAdmin.Application.DTOs.AuthDtos;

namespace NextAdmin.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IRedisService _redisService;

    public AuthController(
        IAuthService authService,
        IRedisService redisService)
    {
        _authService = authService;
        _redisService = redisService;
    }
    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证响应</returns>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(request, cancellationToken);
        
        //if (result.Success)
        //{
        //    // 记录操作日志
        //    await _systemLogService.LogOperationAsync(
        //        "AuthController.Login",
        //        $"用户登录: {request.UserName}"
        //    );
        //}
        //else
        //{
        //    // 记录错误日志
        //    await _systemLogService.LogErrorAsync(
        //        "AuthController.Login",
        //        $"用户登录失败: {request.UserName}",
        //        result.Message ?? ""
        //    );
        //}
        
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="request">注册请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证响应</returns>
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(request, cancellationToken);
        
        //if (result.Success)
        //{
        //    // 记录操作日志
        //    await _systemLogService.LogOperationAsync(
        //        "AuthController.Register",
        //        $"用户注册: {request.UserName}"
        //    );
        //}
        //else
        //{
        //    // 记录错误日志
        //    await _systemLogService.LogErrorAsync(
        //        "AuthController.Register",
        //        $"用户注册失败: {request.UserName}",
        //        result.Message ?? ""
        //    );
        //}
        
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    /// <param name="request">刷新令牌请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证响应</returns>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RefreshTokenAsync(request, cancellationToken);
        
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "无效的用户" });
        }

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        var token = authHeader is not null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader.Substring("Bearer ".Length).Trim()
            : string.Empty;

        DateTime? expiresAt = null;
        string tokenId = string.Empty;
        if (!string.IsNullOrEmpty(token))
        {
            if (TryGetTokenExpiry(token, out var parsedExpiry))
            {
                expiresAt = parsedExpiry;
            }

            if (TryGetTokenId(token, out var parsedTokenId))
            {
                tokenId = parsedTokenId;
            }
        }

        var result = await _authService.LogoutAsync(userId, cancellationToken);

        if (result && !string.IsNullOrEmpty(tokenId) && expiresAt.HasValue)
        {
            var ttl = expiresAt.Value - DateTime.UtcNow;
            if (ttl > TimeSpan.Zero)
            {
                await _redisService.SetStringAsync($"auth:token:revoked:{tokenId}", "revoked", ttl);
            }
        }

        //if (result)
        //{
        //    // 记录操作日志
        //    await _systemLogService.LogOperationAsync(
        //        "AuthController.Logout",
        //        $"用户登出: UserID={userId}"
        //    );
        //}

        return result
            ? Ok(new { message = "登出成功" })
            : BadRequest(new { message = "登出失败" });
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息</returns>
    [HttpGet("userinfo")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<object>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInfo>> GetUserInfo(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "无效的用户" });
        }

        var userInfo = await _authService.GetUserInfoAsync(userId, cancellationToken);
        
        return userInfo is not null 
            ? Ok(userInfo) 
            : NotFound(new { message = "用户不存在" });
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="request">修改密码请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("change-password")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "无效的用户" });
        }

        var result = await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        
        //if (result)
        //{
        //    // 记录操作日志
        //    await _systemLogService.LogOperationAsync(
        //        "AuthController.ChangePassword",
        //        $"修改密码: UserID={userId}"
        //    );
        //}
        //else
        //{
        //    // 记录错误日志
        //    await _systemLogService.LogErrorAsync(
        //        "AuthController.ChangePassword",
        //        $"修改密码失败: UserID={userId}",
        //        "密码修改失败"
        //    );
        //}
        
        return result 
            ? Ok(new { message = "密码修改成功" })
            : BadRequest(new { message = "密码修改失败" });
    }

    /// <summary>
    /// 验证TOKEN是否有效（从Redis中验证）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>TOKEN验证结果</returns>
    [HttpGet("validate-token")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ValidateToken(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "无效的用户" });
        }

        // 从请求头中获取TOKEN
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return BadRequest(new { message = "缺少有效的TOKEN" });
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "缺少有效的TOKEN" });
        }

        if (!TryGetTokenId(token, out var tokenId))
        {
            return BadRequest(new { message = "TOKEN验证失败", valid = false });
        }

        if (!TryGetTokenExpiry(token, out var expiresAt))
        {
            return BadRequest(new { message = "TOKEN验证失败", valid = false });
        }

        if (!string.IsNullOrEmpty(tokenId))
        {
            var revokedMarker = await _redisService.GetStringAsync($"auth:token:revoked:{tokenId}");
            if (!string.IsNullOrEmpty(revokedMarker))
            {
                return BadRequest(new { message = "TOKEN已失效", valid = false });
            }
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "TOKEN已过期", valid = false });
        }

        var isValid = await _authService.ValidateTokenFromRedisAsync(userId, token, cancellationToken);
        if (isValid)
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { valid = true }, "TOKEN验证成功"));
        }

        var restored = await _authService.StoreTokenInRedisAsync(userId, token, expiresAt, cancellationToken);
        if (!restored)
        {
            return BadRequest(new { message = "TOKEN验证失败", valid = false });
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { valid = true, restored = true, expiresAt }, "TOKEN验证成功"));
    }

    private static bool TryGetTokenExpiry(string token, out DateTime expiresAt)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parsedToken = handler.ReadJwtToken(token);
            expiresAt = parsedToken.ValidTo;
            return true;
        }
        catch
        {
            expiresAt = DateTime.MinValue;
            return false;
        }
    }

    private static bool TryGetTokenId(string token, out string tokenId)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parsedToken = handler.ReadJwtToken(token);
            tokenId = parsedToken.Id ?? string.Empty;
            return true;
        }
        catch
        {
            tokenId = string.Empty;
            return false;
        }
    }
}
