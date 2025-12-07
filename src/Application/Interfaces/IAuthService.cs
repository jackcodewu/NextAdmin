using NextAdmin.Application.DTOs;
using static NextAdmin.Application.DTOs.AuthDtos;

namespace NextAdmin.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserInfo?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    
    // Redis TOKEN 管理方法
    Task<bool> StoreTokenInRedisAsync(string userId, string token, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task<string?> GetTokenFromRedisAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> RemoveTokenFromRedisAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenFromRedisAsync(string userId, string token, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(CancellationToken cancellationToken = default);
}
