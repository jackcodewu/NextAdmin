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
    /// User login
    /// </summary>
    /// <param name="request">Login request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response</returns>
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
        //    // Log operation
        //    await _systemLogService.LogOperationAsync(
        //        "AuthController.Login",
        //        $"User login: {request.UserName}"
        //    );
        //}
        //else
        //{
        //    // Log error
        //    await _systemLogService.LogErrorAsync(
        //        "AuthController.Login",
        //        $"User login failed: {request.UserName}",
        //        result.Message ?? ""
        //    );
        //}
        
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// User registration
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response</returns>
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
        //    // Log operation
        //    await _systemLogService.LogOperationAsync(
        //        "AuthController.Register",
        //        $"User registration: {request.UserName}"
        //    );
        //}
        //else
        //{
        //    // Log error
        //    await _systemLogService.LogErrorAsync(
        //        "AuthController.Register",
        //        $"User registration failed: {request.UserName}",
        //        result.Message ?? ""
        //    );
        //}
        
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response</returns>
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
    /// User logout
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "Invalid user" });
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
        //    // Log operation
        //    await _systemLogService.LogOperationAsync(
        //        "AuthController.Logout",
        //        $"User logout: UserID={userId}"
        //    );
        //}

        return result
            ? Ok(new { message = "Logout successful" })
            : BadRequest(new { message = "Logout failed" });
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
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
            return BadRequest(new { message = "Invalid user" });
        }

        var userInfo = await _authService.GetUserInfoAsync(userId, cancellationToken);
        
        return userInfo is not null 
            ? Ok(userInfo) 
            : NotFound(new { message = "User does not exist" });
    }

    /// <summary>
    /// Change password
    /// </summary>
    /// <param name="request">Change password request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
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
            return BadRequest(new { message = "Invalid user" });
        }

        var result = await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        
        //if (result)
        //{
        //    // Log operation
        //    await _systemLogService.LogOperationAsync(
        //        "AuthController.ChangePassword",
        //        $"Password changed: UserID={userId}"
        //    );
        //}
        //else
        //{
        //    // Log error
        //    await _systemLogService.LogErrorAsync(
        //        "AuthController.ChangePassword",
        //        $"Password change failed: UserID={userId}",
        //        "Password change failed"
        //    );
        //}
        
        return result 
            ? Ok(new { message = "Password changed successfully" })
            : BadRequest(new { message = "Password change failed" });
    }

    /// <summary>
    /// Validate if TOKEN is valid (verify from Redis)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>TOKEN validation result</returns>
    [HttpGet("validate-token")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ValidateToken(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "Invalid user" });
        }

        // Get TOKEN from request header
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return BadRequest(new { message = "Missing valid TOKEN" });
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Missing valid TOKEN" });
        }

        if (!TryGetTokenId(token, out var tokenId))
        {
            return BadRequest(new { message = "TOKEN validation failed", valid = false });
        }

        if (!TryGetTokenExpiry(token, out var expiresAt))
        {
            return BadRequest(new { message = "TOKEN validation failed", valid = false });
        }

        if (!string.IsNullOrEmpty(tokenId))
        {
            var revokedMarker = await _redisService.GetStringAsync($"auth:token:revoked:{tokenId}");
            if (!string.IsNullOrEmpty(revokedMarker))
            {
                return BadRequest(new { message = "TOKEN has been revoked", valid = false });
            }
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "TOKEN has expired", valid = false });
        }

        var isValid = await _authService.ValidateTokenFromRedisAsync(userId, token, cancellationToken);
        if (isValid)
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { valid = true }, "TOKEN validation successful"));
        }

        var restored = await _authService.StoreTokenInRedisAsync(userId, token, expiresAt, cancellationToken);
        if (!restored)
        {
            return BadRequest(new { message = "TOKEN validation failed", valid = false });
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { valid = true, restored = true, expiresAt }, "TOKEN validation successful"));
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
