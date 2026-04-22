using ResumeAI.Section.API.Entities;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Section.API.Interfaces;

public interface ISectionRepository
{
    Task<IList<ResumeSection>> FindByResumeIdAsync(int resumeId);
    Task<IList<ResumeSection>> FindByResumeIdAndSectionTypeAsync(int resumeId, SectionType sectionType);
    Task<ResumeSection?> FindBySectionIdAsync(int sectionId);
    Task<IList<ResumeSection>> FindByResumeIdOrderByDisplayOrderAsync(int resumeId);
    Task<IList<ResumeSection>> FindByAiGeneratedAsync(bool aiGenerated);
    Task<int> CountByResumeIdAsync(int resumeId);
    Task<ResumeSection> AddAsync(ResumeSection section);
    Task<ResumeSection> UpdateAsync(ResumeSection section);
    Task UpdateDisplayOrderAsync(int sectionId, int displayOrder);
    Task DeleteByResumeIdAsync(int resumeId);
    Task DeleteBySectionIdAsync(int sectionId);
    Task MarkAsAiGeneratedAsync(int sectionId);
    Task UpdateRangeAsync(IEnumerable<ResumeSection> sections);
}
