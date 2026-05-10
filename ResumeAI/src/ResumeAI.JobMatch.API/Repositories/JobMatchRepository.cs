using Microsoft.EntityFrameworkCore;
using ResumeAI.JobMatch.API.Data;
using ResumeAI.JobMatch.API.Entities;
using ResumeAI.JobMatch.API.Interfaces;

namespace ResumeAI.JobMatch.API.Repositories;

public class JobMatchRepository(JobMatchDbContext db) : IJobMatchRepository
{
    public async Task<IList<ResumeAI.JobMatch.API.Entities.JobMatch>> FindByResumeId(int resumeId)
        => await db.JobMatches.Where(m => m.ResumeId == resumeId)
               .OrderByDescending(m => m.MatchScore).ToListAsync();

    public async Task<IList<ResumeAI.JobMatch.API.Entities.JobMatch>> FindByUserId(int userId)
        => await db.JobMatches.Where(m => m.UserId == userId)
               .OrderByDescending(m => m.MatchedAt).ToListAsync();

    public async Task<ResumeAI.JobMatch.API.Entities.JobMatch?> FindByMatchId(int matchId)
        => await db.JobMatches.FindAsync(matchId);

    public async Task<IList<ResumeAI.JobMatch.API.Entities.JobMatch>> FindByMatchScoreGreaterThan(int minScore)
        => await db.JobMatches.Where(m => m.MatchScore > minScore)
               .OrderByDescending(m => m.MatchScore).ToListAsync();

    public async Task<IList<ResumeAI.JobMatch.API.Entities.JobMatch>> FindByIsBookmarked(int userId, bool bookmarked)
        => await db.JobMatches.Where(m => m.UserId == userId && m.IsBookmarked == bookmarked)
               .ToListAsync();

    public async Task<IList<ResumeAI.JobMatch.API.Entities.JobMatch>> FindByJobTitle(string jobTitle)
        => await db.JobMatches.Where(m => m.JobTitle.Contains(jobTitle))
               .ToListAsync();

    public async Task<int> CountByUserId(int userId)
        => await db.JobMatches.CountAsync(m => m.UserId == userId);

    public async Task<ResumeAI.JobMatch.API.Entities.JobMatch> Add(ResumeAI.JobMatch.API.Entities.JobMatch match)
    {
        db.JobMatches.Add(match);
        await db.SaveChangesAsync();
        return match;
    }

    public async Task BookmarkMatch(int matchId, bool isBookmarked)
        => await db.JobMatches
             .Where(m => m.MatchId == matchId)
             .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsBookmarked, isBookmarked));

    public async Task DeleteByMatchId(int matchId)
        => await db.JobMatches.Where(m => m.MatchId == matchId).ExecuteDeleteAsync();
}
