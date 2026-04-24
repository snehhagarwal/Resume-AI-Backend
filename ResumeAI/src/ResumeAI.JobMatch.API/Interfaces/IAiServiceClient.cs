using ResumeAI.Shared.DTOs;

namespace ResumeAI.JobMatch.API.Interfaces;

public interface IAiServiceClient
{
    Task<JobMatchAiResponse?> AnalyzeJobFit(int resumeId, string jobDescription);
}

public record JobMatchAiResponse(int MatchScore, string MissingSkills, string Recommendations);
