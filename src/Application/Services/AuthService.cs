using NextAdmin.Application.Constants;
using NextAdmin.Application.Interfaces;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Log;
using NextAdmin.Redis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static NextAdmin.Application.DTOs.AuthDtos;
using static System.Net.WebRequestMethods;

namespace NextAdmin.Application.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<JwtSettings> jwtOptions,
    IRedisService redisService,
    IHttpContextAccessor httpContextAccessor,
    ICaptchaService captchaService) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;
    private readonly ICaptchaService _captchaService = captchaService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisService _redisService = redisService;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {

            if (!request.IsMobile)
            { // 1. 校验滑动拼图验证码
                if (string.IsNullOrEmpty(request.CaptchaToken) || string.IsNullOrEmpty(request.CaptchaTrack))
                {
                    LogHelper.Warn("登录失败：缺少验证码参数");
                    return new AuthResponse(false, "请先完成滑动拼图验证");
                }
                List<int> trackList;
                try
                {
                    trackList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(request.CaptchaTrack) ?? new List<int>();
                }
                catch
                {
                    LogHelper.Warn("登录失败：验证码轨迹格式错误");
                    return new AuthResponse(false, "验证码轨迹格式错误");
                }
                var captchaValid = await _captchaService.VerifyCaptchaAsync(new DTOs.Captcha.CaptchaVerifyDto
                {
                    Token = request.CaptchaToken,
                    X = (int)request.CaptchaX,
                    Track = trackList
                }, true);

                if (!captchaValid)
                {
                    LogHelper.Warn("登录失败：滑动拼图验证码校验未通过");
                    return new AuthResponse(false, "滑动拼图验证未通过");
                }
            }

            var user = await userManager.FindByNameAsync(request.UserName);
            if (user is null)
            {
                LogHelper.Warn("登录失败：用户 {UserName} 不存在", request.UserName);
                return new AuthResponse(false, "用户名或密码错误");
            }

            if (!user.IsActive)
            {
                LogHelper.Warn("登录失败：用户 {UserName} 已被禁用", request.UserName);
                return new AuthResponse(false, "账户已被禁用");
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                LogHelper.Warn("登录失败：用户 {UserName} 密码错误", request.UserName);
                return new AuthResponse(false, "用户名或密码错误");
            }

            // 更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            // 生成 JWT Token
            var token = await GenerateJwtTokenAsync(user, cancellationToken);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);
            var userInfo = await GetUserInfoAsync(user.Id.ToString(), cancellationToken);

            // 将TOKEN存储到Redis中
            var tokenStored = await StoreTokenInRedisAsync(user.Id.ToString(), token, expiresAt, cancellationToken);
            if (!tokenStored)
            {
                LogHelper.Warn("TOKEN存储到Redis失败，但登录继续：{UserName}", request.UserName);
            }

            LogHelper.Info("用户 {UserName} 登录成功", request.UserName);

            return new AuthResponse(
                true,
                "登录成功",
                token,
                expiresAt,
                userInfo
            );
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, $"登录过程中发生错误：{request.UserName}");
            return new AuthResponse(false, "登录过程中发生错误");
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查用户名是否已存在
            var existingUser = await userManager.FindByNameAsync(request.UserName);
            if (existingUser is not null)
            {
                return new AuthResponse(false, "用户名已存在");
            }

            // 检查邮箱是否已存在
            var existingEmail = await userManager.FindByEmailAsync(request.Email);
            if (existingEmail is not null)
            {
                return new AuthResponse(false, "邮箱已被注册");
            }

            // 创建新用户
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = $"{request.FirstName} {request.LastName}".Trim(),
                Department = request.Department,
                Position = request.Position,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                LogHelper.Warn($"注册失败：{request.UserName}, 错误：{errors}");
                return new AuthResponse(false, $"注册失败: {errors}");
            }

            // 分配默认角色
            await userManager.AddToRoleAsync(user, "User");

            LogHelper.Info("用户注册成功：{UserName}", request.UserName);

            return new AuthResponse(true, "注册成功");
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "注册过程中发生错误：{UserName}", request.UserName);
            return new AuthResponse(false, "注册过程中发生错误");
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = false, // 不验证过期时间
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(request.Token, tokenValidationParameters, out var validatedToken);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return new AuthResponse(false, "无效的令牌");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user is null || !user.IsActive)
            {
                return new AuthResponse(false, "用户不存在或已被禁用");
            }

            // 生成新的 JWT Token
            var newToken = await GenerateJwtTokenAsync(user, cancellationToken);
            var userInfo = await GetUserInfoAsync(user.Id.ToString(), cancellationToken);

            return new AuthResponse(
                true,
                "令牌刷新成功",
                newToken,
                DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
                userInfo
            );
        }
        catch (Exception ex)
        {
           LogHelper.Error(ex, "刷新令牌失败");
            return new AuthResponse(false, "刷新令牌失败");
        }
    }

    public async Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1) 从请求读取当前 Bearer Token
            var rawAuth = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();
            var token = string.IsNullOrWhiteSpace(rawAuth) ? null : rawAuth.Replace("Bearer ", "");

            // 2) 若能读到 Token，则解析 JTI 与过期时间并写入吊销表（带TTL）
            if (!string.IsNullOrEmpty(token) && TryParseTokenMeta(token, out var jti, out var expiresAtUtc))
            {
                var ttl = expiresAtUtc - DateTime.UtcNow;
                if (ttl > TimeSpan.Zero)
                {
                    var revokedKey = $"auth:token:revoked:{jti}";
                    await _redisService.SetStringAsync(revokedKey, "1", ttl);
                    LogHelper.Info($"已写入吊销标记：{revokedKey}，TTL：{ttl}");
                }
            }

            // 3) 移除用户在 Redis 中的当前 TOKEN（auth:tokens:{userId} Hash 中的特定 JTI）
            var tokenRemoved = await RemoveTokenFromRedisAsync(userId, cancellationToken);
            if (!tokenRemoved)
            {
                LogHelper.Warn("从Redis移除TOKEN失败，但登出继续：{UserId}", userId);
            }

            // 4) 清理 Identity 登录状态（如果使用了）
            await signInManager.SignOutAsync();
            LogHelper.Info("用户登出（当前设备）：{UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "登出失败：{UserId}", userId);
            return false;
        }
    }
    public async Task<bool> LogoutAsync( CancellationToken cancellationToken = default)
    {
        var userId = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            LogHelper.Warn("登出失败：无法获取当前用户ID");
            return false;
        }

        try
        {
            // 1) 从请求读取当前 Bearer Token
            var rawAuth = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();
            var token = string.IsNullOrWhiteSpace(rawAuth) ? null : rawAuth.Replace("Bearer ", "");

            // 2) 若能读到 Token，则解析 JTI 与过期时间并写入吊销表（带TTL）
            if (!string.IsNullOrEmpty(token) && TryParseTokenMeta(token, out var jti, out var expiresAtUtc))
            {
                var ttl = expiresAtUtc - DateTime.UtcNow;
                if (ttl > TimeSpan.Zero)
                {
                    var revokedKey = $"auth:token:revoked:{jti}";
                    await _redisService.SetStringAsync(revokedKey, "1", ttl);
                    LogHelper.Info($"已写入吊销标记：{revokedKey}，TTL：{ttl}");
                }
            }

            // 3) 移除用户在 Redis 中的当前 TOKEN（auth:tokens:{userId} Hash 中的特定 JTI）
            var tokenRemoved = await RemoveTokenFromRedisAsync(userId, cancellationToken);
            if (!tokenRemoved)
            {
                LogHelper.Warn("从Redis移除TOKEN失败，但登出继续：{UserId}", userId);
            }

            // 4) 清理 Identity 登录状态（如果使用了）
            await signInManager.SignOutAsync();
            LogHelper.Info("用户登出（当前设备）：{UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "登出失败：{UserId}", userId);
            return false;
        }
    }
    public async Task<UserInfo?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var objectId))
            {
                return null;
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return null;
            }

            var roles = await userManager.GetRolesAsync(user);

            return new UserInfo(
                user.Id.ToString(),
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName,
                user.DisplayName,
                user.Department,
                user.Position,
                user.TenantId.ToString(),
                [.. roles],
                user.CreatedAt,
                user.LastLoginAt
            );
        }
        catch (Exception ex)
        {
           LogHelper.Error(ex, "获取用户信息失败：{UserId}", userId);
            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return false;
            }

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (result.Succeeded)
            {
                LogHelper.Info("用户密码修改成功：{UserName}", user.UserName);
                return true;
            }

            LogHelper.Warn("用户密码修改失败：{UserName}", user.UserName);
            return false;
        }
        catch (Exception ex)
        {
           LogHelper.Error(ex, "修改密码失败：{UserId}", userId);
            return false;
        }
    }

    #region Redis TOKEN 管理方法

    /// <summary>
    /// 将TOKEN存储到Redis中（支持多地登录）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="token">JWT TOKEN</param>
    /// <param name="expiresAt">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存储成功</returns>
    public async Task<bool> StoreTokenInRedisAsync(string userId, string token, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiry = expiresAt - DateTime.UtcNow;
            
            if (expiry <= TimeSpan.Zero)
            {
                LogHelper.Warn("TOKEN已过期，无法存储到Redis：{UserId}", userId);
                return false;
            }

            // 从TOKEN中提取JTI（唯一标识）
            if (!TryParseTokenMeta(token, out var jti, out _))
            {
                LogHelper.Warn("无法解析TOKEN的JTI，存储失败：{UserId}", userId);
                return false;
            }

            // 使用 Hash 结构存储多个TOKEN：key = auth:tokens:{userId}, field = {jti}, value = {token}
            var hashKey = $"auth:tokens:{userId}";
            var tokenData = new
            {
                token,
                expiresAt = expiresAt.ToString("o"), // ISO 8601 格式
                createdAt = DateTime.UtcNow.ToString("o"),
                jti
            };
            
            // 将TOKEN数据序列化为JSON并存储到Hash中
            var tokenJson = System.Text.Json.JsonSerializer.Serialize(tokenData);
            await _redisService.SetHashAsync(hashKey, jti, tokenJson);
            
            // 为整个Hash设置过期时间（使用最长的TOKEN过期时间 + 1小时缓冲）
            await _redisService.SetExpiryAsync(hashKey, expiry.Add(TimeSpan.FromHours(1)));
            
            LogHelper.Info($"TOKEN已存储到Redis（多地登录）：{userId}, JTI: {jti}, 过期时间：{expiresAt}");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "存储TOKEN到Redis时发生错误：{UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 从Redis中获取用户的所有TOKEN（已废弃，保留用于向后兼容）
    /// 注意：新版本使用 Hash 结构存储多个 TOKEN，此方法仅用于兼容性
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>TOKEN字符串（仅返回第一个找到的），如果不存在则返回null</returns>
    [Obsolete("此方法已过时，新版本支持多地登录，请使用 ValidateTokenFromRedisAsync 验证特定 TOKEN")]
    public async Task<string?> GetTokenFromRedisAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 新版本：从 Hash 中获取所有 TOKEN，返回第一个
            var hashKey = $"auth:tokens:{userId}";
            var db = _redisService.GetDatabase();
            var allFields = await db.HashKeysAsync(hashKey);
            
            if (allFields.Length > 0)
            {
                var firstJti = allFields[0].ToString();
                var tokenJson = await _redisService.GetHashAsync(hashKey, firstJti);
                
                if (!string.IsNullOrEmpty(tokenJson))
                {
                    var tokenData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(tokenJson);
                    var token = tokenData.GetProperty("token").GetString();
                    LogHelper.Info($"从Redis获取TOKEN成功（多地登录模式，返回第一个）：{userId}");
                    return token;
                }
            }
            
            LogHelper.Info($"Redis中未找到TOKEN：{userId}");
            return null;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, $"从Redis获取TOKEN时发生错误：{userId}");
            return null;
        }
    }

    /// <summary>
    /// 从Redis中移除当前TOKEN（支持多地登录）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否移除成功</returns>
    public async Task<bool> RemoveTokenFromRedisAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 从请求中获取当前TOKEN
            var rawAuth = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();
            var token = string.IsNullOrWhiteSpace(rawAuth) ? null : rawAuth.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                LogHelper.Warn("无法获取当前TOKEN，移除失败：{UserId}", userId);
                return false;
            }

            // 从TOKEN中提取JTI
            if (!TryParseTokenMeta(token, out var jti, out _))
            {
                LogHelper.Warn("无法解析TOKEN的JTI，移除失败：{UserId}", userId);
                return false;
            }

            // 从Hash中删除该JTI对应的TOKEN
            var hashKey = $"auth:tokens:{userId}";
            var result = await _redisService.DeleteHashFieldAsync(hashKey, jti);
            
            if (result)
            {
                LogHelper.Info($"TOKEN已从Redis移除（JTI: {jti}）：{userId}");
            }
            else
            {
                LogHelper.Warn($"从Redis移除TOKEN失败（JTI: {jti}）：{userId}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "从Redis移除TOKEN时发生错误：{UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 从Redis中移除用户的所有TOKEN（踢出所有登录会话）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否移除成功</returns>
    public async Task<bool> RemoveAllTokensFromRedisAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hashKey = $"auth:tokens:{userId}";
            var result = await _redisService.DeleteAsync(hashKey);
            
            if (result)
            {
                LogHelper.Info("用户所有TOKEN已从Redis移除：{UserId}", userId);
            }
            else
            {
                LogHelper.Warn("从Redis移除用户所有TOKEN失败：{UserId}", userId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "从Redis移除用户所有TOKEN时发生错误：{UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 验证Redis中的TOKEN是否有效（支持多地登录）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="token">要验证的TOKEN</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>TOKEN是否有效</returns>
    public async Task<bool> ValidateTokenFromRedisAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // 从TOKEN中提取JTI
            if (!TryParseTokenMeta(token, out var jti, out _))
            {
                LogHelper.Warn("无法解析TOKEN的JTI，验证失败：{UserId}", userId);
                return false;
            }

            // 从Hash中获取该JTI对应的TOKEN数据
            var hashKey = $"auth:tokens:{userId}";
            var tokenJson = await _redisService.GetHashAsync(hashKey, jti);
            
            if (string.IsNullOrEmpty(tokenJson))
            {
                LogHelper.Info($"Redis中未找到TOKEN（JTI: {jti}），验证失败：{userId}");
                return false;
            }

            // 解析TOKEN数据
            var tokenData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(tokenJson);
            var storedToken = tokenData.GetProperty("token").GetString();
            var expiresAtStr = tokenData.GetProperty("expiresAt").GetString();
            
            // 验证TOKEN是否匹配
            if (storedToken != token)
            {
                LogHelper.Warn($"TOKEN不匹配（JTI: {jti}），验证失败：{userId}");
                return false;
            }

            // 验证TOKEN是否过期
            if (DateTime.TryParse(expiresAtStr, out var expiresAt) && expiresAt <= DateTime.UtcNow)
            {
                LogHelper.Info($"TOKEN已过期（JTI: {jti}），验证失败：{userId}");
                // 清理过期的TOKEN
                await _redisService.DeleteHashFieldAsync(hashKey, jti);
                return false;
            }

            LogHelper.Info($"TOKEN验证成功（JTI: {jti}）：{userId}");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "验证Redis中的TOKEN时发生错误：{UserId}", userId);
            return false;
        }
    }

    #endregion
    private static bool TryParseTokenMeta(string token, out string jti, out DateTime expiresAtUtc)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            jti = jwt.Id ?? string.Empty;
            expiresAtUtc = jwt.ValidTo; // 一般是UTC
            return !string.IsNullOrEmpty(jti) && expiresAtUtc > DateTime.UtcNow;
        }
        catch
        {
            jti = string.Empty;
            expiresAtUtc = DateTime.MinValue;
            return false;
        }
    }


    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        
        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new ("TenantId",user.TenantId.ToString()),
            new ("TenantName",user.TenantName),
            new("DisplayName", user.DisplayName ?? string.Empty),
            new("Department", user.Department ?? string.Empty),
            new("Position", user.Position ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // 添加角色声明
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // 添加权限声明
        var permissions = new HashSet<string>();
        bool isSystem = true;
        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role?.PermissionCodes != null)
            {
                permissions.UnionWith(role.PermissionCodes);
            }
            isSystem = role.IsSystemRole & isSystem;
        }

        claims.Add(new Claim("IsSystem", isSystem.ToString().ToLower()));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
