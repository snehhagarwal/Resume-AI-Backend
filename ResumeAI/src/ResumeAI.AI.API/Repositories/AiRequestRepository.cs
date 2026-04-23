using Microsoft.EntityFrameworkCore;
using ResumeAI.AI.API.Data;
using ResumeAI.AI.API.Entities;
using ResumeAI.Shared.Enums;
using ResumeAI.AI.API.Interfaces;

namespace ResumeAI.AI.API.Repositories;

public class AiRequestRepository(AiDbContext db) : IAiRequestRepository
{
    public Task<IList<AiRequest>> FindByUserIdAsync(int userId)
        => db.AiRequests.Where(r => r.UserId == userId)
               .OrderByDescending(r => r.CreatedAt).ToListAsync()
               .ContinueWith(t => (IList<AiRequest>)t.Result);

    public Task<IList<AiRequest>> FindByResumeIdAsync(int resumeId)
        => db.AiRequests.Where(r => r.ResumeId == resumeId)
               .OrderByDescending(r => r.CreatedAt).ToListAsync()
               .ContinueWith(t => (IList<AiRequest>)t.Result);

    public Task<AiRequest?> FindByRequestIdAsync(string requestId)
        => db.AiRequests.FindAsync(requestId).AsTask();

    public Task<IList<AiRequest>> FindByRequestTypeAsync(AiRequestType type)
        => db.AiRequests.Where(r => r.RequestType == type).ToListAsync()
               .ContinueWith(t => (IList<AiRequest>)t.Result);

    public Task<IList<AiRequest>> FindByStatusAsync(AiRequestStatus status)
        => db.AiRequests.Where(r => r.Status == status).ToListAsync()
               .ContinueWith(t => (IList<AiRequest>)t.Result);

    public Task<int> CountByUserIdTodayAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.AiRequests.CountAsync(r =>
            r.UserId == userId &&
            DateOnly.FromDateTime(r.CreatedAt) == today);
    }

    public async Task<long> SumTokensByUserIdAsync(int userId)
    {
        var sum = await db.AiRequests
            .Where(r => r.UserId == userId && r.Status == AiRequestStatus.COMPLETED)
            .SumAsync(r => (long)r.TokensUsed);
        return sum;
    }

    public async Task<AiRequest> AddAsync(AiRequest request)
    {
        db.AiRequests.Add(request);
        await db.SaveChangesAsync();
        return request;
    }

    public async Task<AiRequest> UpdateAsync(AiRequest request)
    {
        db.AiRequests.Update(request);
        await db.SaveChangesAsync();
        return request;
    }
}
