using Microsoft.EntityFrameworkCore;
using ResumeAI.Auth.API.Data;
using ResumeAI.Auth.API.Entities;
using ResumeAI.Auth.API.Interfaces;

namespace ResumeAI.Auth.API.Repositories;

public class RefreshTokenRepository(AuthDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByTokenAsync(string token)
        => db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == token);

    public async Task AddAsync(RefreshToken token)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
    }

    public async Task RevokeByTokenAsync(string token)
    {
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
        if (existing != null)
        {
            existing.IsRevoked = true;
            await db.SaveChangesAsync();
        }
    }

    public async Task RevokeAllForUserAsync(int userId)
    {
        await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true));
    }
}
