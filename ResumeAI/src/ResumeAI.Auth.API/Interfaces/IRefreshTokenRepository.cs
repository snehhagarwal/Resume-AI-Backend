using ResumeAI.Auth.API.Entities;

namespace ResumeAI.Auth.API.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByTokenAsync(string token);
    Task AddAsync(RefreshToken token);
    Task RevokeByTokenAsync(string token);
    Task RevokeAllForUserAsync(int userId);
}
