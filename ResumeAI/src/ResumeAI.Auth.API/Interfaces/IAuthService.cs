using ResumeAI.Auth.API.Entities;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Auth.API.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync(int userId);
    Task LogoutAllAsync(int userId);
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task UpdateSubscriptionAsync(int userId, SubscriptionPlan plan);
    Task DeactivateAccountAsync(int userId);
    Task ReactivateAccountAsync(int userId);
    Task HardDeleteUserAsync(int userId);
    Task<string> RefreshTokenAsync(string refreshToken);
    bool ValidateToken(string token);
    Task SuspendUserAsync(int userId);
    Task<IList<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Find-or-create a user that authenticated via an external OAuth provider
    /// (Google or LinkedIn) and return a JWT <see cref="AuthResponse"/>.
    /// </summary>
    /// <param name="provider">The provider that authenticated the user.</param>
    /// <param name="email">Email claim from the external identity.</param>
    /// <param name="fullName">Display-name claim from the external identity.</param>
    Task<AuthResponse> OAuthLoginAsync(AuthProvider provider, string email, string fullName);
    Task SyncOAuthProfileAsync(int userId, string fullName, string email);
}
