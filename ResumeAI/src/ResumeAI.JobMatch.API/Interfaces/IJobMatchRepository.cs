using ResumeAI.JobMatch.API.Entities;

namespace ResumeAI.JobMatch.API.Interfaces;

public interface IJobMatchRepository
{
    Task<IList<ResumeAI.JobMatch.API.Entities.JobMatch>> FindByResumeId(int resumeId);
    Task<IList<ResumeAI.JobMatch.API.Entities.JobMatch>> FindByUserId(int userId);
    Task<ResumeAI.JobMatch.API.Entities.JobMatch?> FindByMatchId(int matchId);
    Task<IList<ResumeAI.JobMatch.API.Entities.JobMatch>> FindByMatchScoreGreaterThan(int minScore);
    Task<IList<ResumeAI.JobMatch.API.Entities.JobMatch>> FindByIsBookmarked(int userId, bool bookmarked);
    Task<IList<ResumeAI.JobMatch.API.Entities.JobMatch>> FindByJobTitle(string jobTitle);
    Task<int> CountByUserId(int userId);
    Task<ResumeAI.JobMatch.API.Entities.JobMatch> Add(ResumeAI.JobMatch.API.Entities.JobMatch match);
    Task BookmarkMatch(int matchId, bool isBookmarked);
    Task DeleteByMatchId(int matchId);
}
