using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Resume.API.Interfaces;

public interface IResumeService
{
    Task<ResumeDto> CreateResumeAsync(int userId, SubscriptionPlan plan, CreateResumeRequest request);
    Task<ResumeDto?> GetResumeByIdAsync(int resumeId);
    Task<IList<ResumeDto>> GetResumesByUserAsync(int userId);
    Task<ResumeDto> UpdateResumeAsync(int resumeId, int userId, UpdateResumeRequest request);
    Task DeleteResumeAsync(int resumeId, int userId);
    Task<ResumeDto> DuplicateResumeAsync(int resumeId, int userId);
    Task UpdateAtsScoreAsync(int resumeId, int score);
    Task<ResumeDto> PublishResumeAsync(int resumeId, int userId);
    Task<ResumeDto> UnpublishResumeAsync(int resumeId, int userId);
    Task<IList<ResumeDto>> GetPublicResumesAsync();
    Task IncrementViewCountAsync(int resumeId);
    Task<IList<ResumeDto>> GetResumesByTemplateAsync(int templateId);
    Task ChangeTemplateAsync(int resumeId, int userId, int templateId);
    Task<ResumeDto?> GetResumeWithSectionsAsync(int resumeId);
    Task EnforceResumeLimitAsync(int userId, SubscriptionPlan plan);
}
