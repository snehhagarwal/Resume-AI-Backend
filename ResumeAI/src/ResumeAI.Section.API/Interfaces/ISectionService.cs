using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Section.API.Interfaces;

public interface ISectionService
{
    Task<SectionDto> AddSectionAsync(int userId, AddSectionRequest request);
    Task<IList<SectionDto>> GetSectionsByResumeAsync(int resumeId, int userId);
    Task<SectionDto?> GetSectionByIdAsync(int sectionId);
    Task<IList<SectionDto>> GetSectionsByTypeAsync(int resumeId, SectionType sectionType, int userId);
    Task<SectionDto> UpdateSectionAsync(int sectionId, int userId, UpdateSectionRequest request);
    Task DeleteSectionAsync(int sectionId, int userId);
    Task DeleteAllSectionsAsync(int resumeId, int userId);
    Task ReorderSectionsAsync(int resumeId, int userId, ReorderSectionsRequest request);
    Task<SectionDto> ToggleVisibilityAsync(int sectionId, int userId);
    Task<IList<SectionDto>> BulkUpdateSectionsAsync(int userId, BulkUpdateSectionsRequest request);
    Task MarkAsAiGeneratedAsync(int sectionId, int userId);
    Task<int> CountSectionsByResumeAsync(int resumeId, int userId);
    Task<SectionDto> CopySectionAsync(int sectionId, int userId, int targetResumeId);
}
