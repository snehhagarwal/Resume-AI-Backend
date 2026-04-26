using ResumeAI.Section.API.Entities;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Section.API.Interfaces;

public interface ISectionRepository
{
    Task<IList<ResumeSection>> FindByResumeIdAsync(int resumeId, int userId);
    Task<IList<ResumeSection>> FindByResumeIdAndSectionTypeAsync(int resumeId, SectionType sectionType, int userId);
    Task<ResumeSection?> FindBySectionIdAsync(int sectionId);
    Task<IList<ResumeSection>> FindByResumeIdOrderByDisplayOrderAsync(int resumeId, int userId);
    Task<IList<ResumeSection>> FindByAiGeneratedAsync(bool aiGenerated);
    Task<int> CountByResumeIdAsync(int resumeId, int userId);
    Task<ResumeSection> AddAsync(ResumeSection section);
    Task<ResumeSection> UpdateAsync(ResumeSection section);
    Task UpdateDisplayOrderAsync(int sectionId, int userId, int displayOrder);
    Task DeleteByResumeIdAsync(int resumeId, int userId);
    Task DeleteBySectionIdAsync(int sectionId, int userId);
    Task MarkAsAiGeneratedAsync(int sectionId, int userId);
    Task UpdateRangeAsync(IEnumerable<ResumeSection> sections);
}
