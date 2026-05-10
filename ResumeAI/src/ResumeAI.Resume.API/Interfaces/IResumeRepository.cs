using ResumeAI.Resume.API.Entities;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Resume.API.Interfaces;

public interface IResumeRepository
{
    Task<ResumeRecord?> FindByResumeIdAsync(int resumeId);
    Task<IList<ResumeRecord>> FindByUserIdAsync(int userId);
    Task<IList<ResumeRecord>> FindByStatusAsync(ResumeStatus status);
    Task<IList<ResumeRecord>> FindByTargetJobTitleAsync(string targetJobTitle);
    Task<IList<ResumeRecord>> FindByIsPublicAsync(bool isPublic);
    Task<int> CountByUserIdAsync(int userId);
    Task<IList<ResumeRecord>> FindByTemplateIdAsync(int templateId);
    Task<ResumeRecord> AddAsync(ResumeRecord resume);
    Task<ResumeRecord> UpdateAsync(ResumeRecord resume);
    Task UpdateAtsScoreAsync(int resumeId, int score);
    Task IncrementViewCountAsync(int resumeId);
    Task DeleteByResumeIdAsync(int resumeId);
    Task<ResumeRecord?> FindWithSectionsAsync(int resumeId);
}
