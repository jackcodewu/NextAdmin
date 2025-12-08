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
            { // 1. Validate sliding puzzle captcha
                if (string.IsNullOrEmpty(request.CaptchaToken) || string.IsNullOrEmpty(request.CaptchaTrack))
                {
                    LogHelper.Warn("Login failed: Missing captcha parameters");
                    return new AuthResponse(false, "Please complete the sliding puzzle verification");
                }
                List<int> trackList;
                try
                {
                    trackList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(request.CaptchaTrack) ?? new List<int>();
                }
                catch
                {
                    LogHelper.Warn("Login failed: Invalid captcha track format");
                    return new AuthResponse(false, "Invalid captcha track format");
                }
                var captchaValid = await _captchaService.VerifyCaptchaAsync(new DTOs.Captcha.CaptchaVerifyDto
                {
                    Token = request.CaptchaToken,
                    X = (int)request.CaptchaX,
                    Track = trackList
                }, true);

                if (!captchaValid)
                {
                    LogHelper.Warn("Login failed: Sliding puzzle captcha verification failed");
                    return new AuthResponse(false, "Sliding puzzle verification failed");
                }
            }

            var user = await userManager.FindByNameAsync(request.UserName);
            if (user is null)
            {
                LogHelper.Warn("Login failed: User {UserName} does not exist", request.UserName);
                return new AuthResponse(false, "Incorrect username or password");
            }

            if (!user.IsActive)
            {
                LogHelper.Warn("Login failed: User {UserName} has been disabled", request.UserName);
                return new AuthResponse(false, "Account has been disabled");
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                LogHelper.Warn("Login failed: Incorrect password for user {UserName}", request.UserName);
                return new AuthResponse(false, "Incorrect username or password");
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            // Generate JWT Token
            var token = await GenerateJwtTokenAsync(user, cancellationToken);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);
            var userInfo = await GetUserInfoAsync(user.Id.ToString(), cancellationToken);

            // Store TOKEN in Redis
            var tokenStored = await StoreTokenInRedisAsync(user.Id.ToString(), token, expiresAt, cancellationToken);
            if (!tokenStored)
            {
                LogHelper.Warn("Failed to store TOKEN in Redis, but login continues: {UserName}", request.UserName);
            }

            LogHelper.Info("User {UserName} logged in successfully", request.UserName);

            return new AuthResponse(
                true,
                "Login successful",
                token,
                expiresAt,
                userInfo
            );
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, $"An error occurred during login: {request.UserName}");
            return new AuthResponse(false, "An error occurred during login");
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if username already exists
            var existingUser = await userManager.FindByNameAsync(request.UserName);
            if (existingUser is not null)
            {
                return new AuthResponse(false, "Username already exists");
            }

            // Check if email already exists
            var existingEmail = await userManager.FindByEmailAsync(request.Email);
            if (existingEmail is not null)
            {
                return new AuthResponse(false, "Email is already registered");
            }

            // Create new user
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
                LogHelper.Warn($"Registration failed: {request.UserName}, Errors: {errors}");
                return new AuthResponse(false, $"Registration failed: {errors}");
            }

            // Assign default role
            await userManager.AddToRoleAsync(user, "User");

            LogHelper.Info("User registered successfully: {UserName}", request.UserName);

            return new AuthResponse(true, "Registration successful");
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "An error occurred during registration: {UserName}", request.UserName);
            return new AuthResponse(false, "An error occurred during registration");
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
                ValidateLifetime = false, // Do not validate expiration time
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(request.Token, tokenValidationParameters, out var validatedToken);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return new AuthResponse(false, "Invalid token");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user is null || !user.IsActive)
            {
                return new AuthResponse(false, "User does not exist or has been disabled");
            }

            // Generate new JWT Token
            var newToken = await GenerateJwtTokenAsync(user, cancellationToken);
            var userInfo = await GetUserInfoAsync(user.Id.ToString(), cancellationToken);

            return new AuthResponse(
                true,
                "Token refresh successful",
                newToken,
                DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
                userInfo
            );
        }
        catch (Exception ex)
        {
           LogHelper.Error(ex, "Token refresh failed");
            return new AuthResponse(false, "Token refresh failed");
        }
    }

    public async Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1) Read current Bearer Token from request
            var rawAuth = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();
            var token = string.IsNullOrWhiteSpace(rawAuth) ? null : rawAuth.Replace("Bearer ", "");

            // 2) If Token is retrieved, parse JTI and expiration time and add to revocation list (with TTL)
            if (!string.IsNullOrEmpty(token) && TryParseTokenMeta(token, out var jti, out var expiresAtUtc))
            {
                var ttl = expiresAtUtc - DateTime.UtcNow;
                if (ttl > TimeSpan.Zero)
                {
                    var revokedKey = $"auth:token:revoked:{jti}";
                    await _redisService.SetStringAsync(revokedKey, "1", ttl);
                    LogHelper.Info($"Written revocation marker: {revokedKey}, TTL: {ttl}");
                }
            }

            // 3) Remove user's current TOKEN from Redis (specific JTI in auth:tokens:{userId} Hash)
            var tokenRemoved = await RemoveTokenFromRedisAsync(userId, cancellationToken);
            if (!tokenRemoved)
            {
                LogHelper.Warn("Failed to remove TOKEN from Redis, but logout continues: {UserId}", userId);
            }

            // 4) Clear Identity login state (if used)
            await signInManager.SignOutAsync();
            LogHelper.Info("User logged out (current device): {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "Logout failed: {UserId}", userId);
            return false;
        }
    }
    public async Task<bool> LogoutAsync( CancellationToken cancellationToken = default)
    {
        var userId = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            LogHelper.Warn("Logout failed: Unable to get current user ID");
            return false;
        }

        try
        {
            // 1) Read current Bearer Token from request
            var rawAuth = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();
            var token = string.IsNullOrWhiteSpace(rawAuth) ? null : rawAuth.Replace("Bearer ", "");

            // 2) If Token is retrieved, parse JTI and expiration time and add to revocation list (with TTL)
            if (!string.IsNullOrEmpty(token) && TryParseTokenMeta(token, out var jti, out var expiresAtUtc))
            {
                var ttl = expiresAtUtc - DateTime.UtcNow;
                if (ttl > TimeSpan.Zero)
                {
                    var revokedKey = $"auth:token:revoked:{jti}";
                    await _redisService.SetStringAsync(revokedKey, "1", ttl);
                    LogHelper.Info($"Written revocation marker: {revokedKey}, TTL: {ttl}");
                }
            }

            // 3) Remove user's current TOKEN from Redis (specific JTI in auth:tokens:{userId} Hash)
            var tokenRemoved = await RemoveTokenFromRedisAsync(userId, cancellationToken);
            if (!tokenRemoved)
            {
                LogHelper.Warn("Failed to remove TOKEN from Redis, but logout continues: {UserId}", userId);
            }

            // 4) Clear Identity login state (if used)
            await signInManager.SignOutAsync();
            LogHelper.Info("User logged out (current device): {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "Logout failed: {UserId}", userId);
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
           LogHelper.Error(ex, "Failed to get user info: {UserId}", userId);
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
                LogHelper.Info("User password changed successfully: {UserName}", user.UserName);
                return true;
            }

            LogHelper.Warn("User password change failed: {UserName}", user.UserName);
            return false;
        }
        catch (Exception ex)
        {
           LogHelper.Error(ex, "Password change failed: {UserId}", userId);
            return false;
        }
    }

    #region Redis TOKEN Management Methods

    /// <summary>
    /// Store TOKEN in Redis (supports multi-location login)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="token">JWT TOKEN</param>
    /// <param name="expiresAt">Expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether storage was successful</returns>
    public async Task<bool> StoreTokenInRedisAsync(string userId, string token, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiry = expiresAt - DateTime.UtcNow;
            
            if (expiry <= TimeSpan.Zero)
            {
                LogHelper.Warn("TOKEN has expired, cannot store in Redis: {UserId}", userId);
                return false;
            }

            // Extract JTI (unique identifier) from TOKEN
            if (!TryParseTokenMeta(token, out var jti, out _))
            {
                LogHelper.Warn("Unable to parse TOKEN JTI, storage failed: {UserId}", userId);
                return false;
            }

            // Use Hash structure to store multiple TOKENs: key = auth:tokens:{userId}, field = {jti}, value = {token}
            var hashKey = $"auth:tokens:{userId}";
            var tokenData = new
            {
                token,
                expiresAt = expiresAt.ToString("o"), // ISO 8601 format
                createdAt = DateTime.UtcNow.ToString("o"),
                jti
            };
            
            // Serialize TOKEN data to JSON and store in Hash
            var tokenJson = System.Text.Json.JsonSerializer.Serialize(tokenData);
            await _redisService.SetHashAsync(hashKey, jti, tokenJson);
            
            // Set expiration time for the entire Hash (using longest TOKEN expiration time + 1 hour buffer)
            await _redisService.SetExpiryAsync(hashKey, expiry.Add(TimeSpan.FromHours(1)));
            
            LogHelper.Info($"TOKEN stored in Redis (multi-location login): {userId}, JTI: {jti}, Expiration: {expiresAt}");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "An error occurred while storing TOKEN in Redis: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Get all TOKENs for user from Redis (deprecated, retained for backward compatibility)
    /// Note: New version uses Hash structure to store multiple TOKENs, this method is only for compatibility
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>TOKEN string (returns only the first found), null if none exists</returns>
    [Obsolete("This method is obsolete, new version supports multi-location login, please use ValidateTokenFromRedisAsync to verify specific TOKEN")]
    public async Task<string?> GetTokenFromRedisAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // New version: Get all TOKENs from Hash, return the first one
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
                    LogHelper.Info($"Successfully retrieved TOKEN from Redis (multi-location login mode, returning first one): {userId}");
                    return token;
                }
            }
            
            LogHelper.Info($"TOKEN not found in Redis: {userId}");
            return null;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, $"An error occurred while retrieving TOKEN from Redis: {userId}");
            return null;
        }
    }

    /// <summary>
    /// Remove current TOKEN from Redis (supports multi-location login)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether removal was successful</returns>
    public async Task<bool> RemoveTokenFromRedisAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current TOKEN from request
            var rawAuth = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();
            var token = string.IsNullOrWhiteSpace(rawAuth) ? null : rawAuth.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                LogHelper.Warn("Unable to get current TOKEN, removal failed: {UserId}", userId);
                return false;
            }

            // Extract JTI from TOKEN
            if (!TryParseTokenMeta(token, out var jti, out _))
            {
                LogHelper.Warn("Unable to parse TOKEN JTI, removal failed: {UserId}", userId);
                return false;
            }

            // Delete TOKEN corresponding to this JTI from Hash
            var hashKey = $"auth:tokens:{userId}";
            var result = await _redisService.DeleteHashFieldAsync(hashKey, jti);
            
            if (result)
            {
                LogHelper.Info($"TOKEN removed from Redis (JTI: {jti}): {userId}");
            }
            else
            {
                LogHelper.Warn($"Failed to remove TOKEN from Redis (JTI: {jti}): {userId}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "An error occurred while removing TOKEN from Redis: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Remove all TOKENs for user from Redis (kick out all login sessions)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether removal was successful</returns>
    public async Task<bool> RemoveAllTokensFromRedisAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hashKey = $"auth:tokens:{userId}";
            var result = await _redisService.DeleteAsync(hashKey);
            
            if (result)
            {
                LogHelper.Info("All TOKENs for user removed from Redis: {UserId}", userId);
            }
            else
            {
                LogHelper.Warn("Failed to remove all TOKENs for user from Redis: {UserId}", userId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "An error occurred while removing all TOKENs for user from Redis: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Validate if TOKEN in Redis is valid (supports multi-location login)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="token">TOKEN to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether TOKEN is valid</returns>
    public async Task<bool> ValidateTokenFromRedisAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract JTI from TOKEN
            if (!TryParseTokenMeta(token, out var jti, out _))
            {
                LogHelper.Warn("Unable to parse TOKEN JTI, validation failed: {UserId}", userId);
                return false;
            }

            // Get TOKEN data corresponding to this JTI from Hash
            var hashKey = $"auth:tokens:{userId}";
            var tokenJson = await _redisService.GetHashAsync(hashKey, jti);
            
            if (string.IsNullOrEmpty(tokenJson))
            {
                LogHelper.Info($"TOKEN not found in Redis (JTI: {jti}), validation failed: {userId}");
                return false;
            }

            // Parse TOKEN data
            var tokenData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(tokenJson);
            var storedToken = tokenData.GetProperty("token").GetString();
            var expiresAtStr = tokenData.GetProperty("expiresAt").GetString();
            
            // Verify TOKEN matches
            if (storedToken != token)
            {
                LogHelper.Warn($"TOKEN mismatch (JTI: {jti}), validation failed: {userId}");
                return false;
            }

            // Verify TOKEN has not expired
            if (DateTime.TryParse(expiresAtStr, out var expiresAt) && expiresAt <= DateTime.UtcNow)
            {
                LogHelper.Info($"TOKEN expired (JTI: {jti}), validation failed: {userId}");
                // Clean up expired TOKEN
                await _redisService.DeleteHashFieldAsync(hashKey, jti);
                return false;
            }

            LogHelper.Info($"TOKEN validation successful (JTI: {jti}): {userId}");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "An error occurred while validating TOKEN in Redis: {UserId}", userId);
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
            expiresAtUtc = jwt.ValidTo; // Typically UTC
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

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Add permission claims
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
