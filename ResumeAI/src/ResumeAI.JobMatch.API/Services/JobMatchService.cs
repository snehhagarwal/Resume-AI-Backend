using ResumeAI.JobMatch.API.Entities;
using ResumeAI.JobMatch.API.Interfaces;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.JobMatch.API.Services;

public class JobMatchService(
    IJobMatchRepository matchRepo,
    IAiServiceClient aiClient,
    IJobSearchClient jobSearchClient,
    ILogger<JobMatchService> logger) : IJobMatchService
{
    public async Task<JobMatchDto> AnalyzeJobFit(int userId, AnalyzeJobFitRequest request)
    {
        var aiResponse = await aiClient.AnalyzeJobFit(request.ResumeId, request.JobDescription);
        
        var match = new ResumeAI.JobMatch.API.Entities.JobMatch
        {
            ResumeId = request.ResumeId,
            UserId = userId,
            JobTitle = request.JobTitle,
            JobDescription = request.JobDescription,
            CompanyName = request.CompanyName,
            Location = request.Location,
            MatchScore = aiResponse?.MatchScore ?? 0,
            MissingSkills = aiResponse?.MissingSkills ?? string.Empty,
            Recommendations = aiResponse?.Recommendations ?? "Unable to generate recommendations at this time.",
            Source = request.Source
        };

        var saved = await matchRepo.Add(match);
        return MapToDto(saved);
    }

    public async Task<IList<JobMatchDto>> GetMatchesByResume(int resumeId)
        => (await matchRepo.FindByResumeId(resumeId)).Select(MapToDto).ToList();

    public async Task<IList<JobMatchDto>> GetMatchesByUser(int userId)
        => (await matchRepo.FindByUserId(userId)).Select(MapToDto).ToList();

    public async Task<JobMatchDto?> GetMatchById(int matchId)
    {
        var m = await matchRepo.FindByMatchId(matchId);
        return m is null ? null : MapToDto(m);
    }

    public async Task<IList<JobMatchDto>> GetTopMatches(int userId, int minScore = 70)
        => (await matchRepo.FindByMatchScoreGreaterThan(minScore))
            .Where(m => m.UserId == userId)
            .Select(MapToDto).ToList();

    public Task BookmarkMatch(int matchId, bool isBookmarked)
        => matchRepo.BookmarkMatch(matchId, isBookmarked);

    public async Task<IList<JobMatchDto>> FetchJobsFromLinkedIn(
        int userId, int resumeId, string keywords)
    {
        return await jobSearchClient.SearchJobs(userId, resumeId, keywords);
    }

    public async Task<IList<JobMatchDto>> FetchJobsFromNaukri(
        int userId, int resumeId, string keywords)
    {
        // Both now use Adzuna as the universal search engine
        return await jobSearchClient.SearchJobs(userId, resumeId, keywords);
    }

    public async Task<string> GetTailoringRecommendations(int matchId)
    {
        var match = await matchRepo.FindByMatchId(matchId)
                    ?? throw new KeyNotFoundException("Match not found.");
        return match.Recommendations;
    }

    public Task DeleteMatch(int matchId)
        => matchRepo.DeleteByMatchId(matchId);

    private static JobMatchDto MapToDto(ResumeAI.JobMatch.API.Entities.JobMatch m) =>
        new(m.MatchId, m.ResumeId, m.UserId, m.JobTitle, m.JobDescription,
            m.CompanyName, m.Location,
            m.MatchScore, m.MissingSkills, m.Recommendations,
            m.Source, m.MatchedAt, m.IsBookmarked);
}
