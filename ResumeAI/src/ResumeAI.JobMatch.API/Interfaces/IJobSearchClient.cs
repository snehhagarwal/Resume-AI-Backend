using ResumeAI.Shared.DTOs;

namespace ResumeAI.JobMatch.API.Interfaces;

public interface IJobSearchClient
{
    Task<IList<JobMatchDto>> SearchJobs(int userId, int resumeId, string keywords);
}
