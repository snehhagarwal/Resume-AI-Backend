using ResumeAI.Shared.DTOs;

namespace ResumeAI.JobMatch.API.Interfaces;

public interface IJobMatchService
{
    Task<JobMatchDto> AnalyzeJobFit(int userId, AnalyzeJobFitRequest request);
    Task<IList<JobMatchDto>> GetMatchesByResume(int resumeId);
    Task<IList<JobMatchDto>> GetMatchesByUser(int userId);
    Task<JobMatchDto?> GetMatchById(int matchId);
    Task<IList<JobMatchDto>> GetTopMatches(int userId, int minScore = 70);
    Task BookmarkMatch(int matchId, bool isBookmarked);
    Task<IList<JobMatchDto>> FetchJobsFromLinkedIn(int userId, int resumeId, string keywords);
    Task<IList<JobMatchDto>> FetchJobsFromNaukri(int userId, int resumeId, string keywords);
    Task<string> GetTailoringRecommendations(int matchId);
    Task DeleteMatch(int matchId);
}
