using ResumeAI.Section.API.Entities;
using ResumeAI.Section.API.Interfaces;
using ResumeAI.Section.API.Repositories;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Section.API.Services;

public class SectionService(ISectionRepository sectionRepo) : ISectionService
{
    public async Task<SectionDto> AddSectionAsync(int userId, AddSectionRequest request)
    {
        var section = new ResumeSection
        {
            ResumeId = request.ResumeId,
            UserId = userId,
            SectionType = request.SectionType,
            Title = request.Title,
            Content = request.Content,
            DisplayOrder = request.DisplayOrder,
            IsVisible = request.IsVisible
        };
        var saved = await sectionRepo.AddAsync(section);
        return MapToDto(saved);
    }

    public async Task<IList<SectionDto>> GetSectionsByResumeAsync(int resumeId, int userId)
    {
        var sections = await sectionRepo.FindByResumeIdOrderByDisplayOrderAsync(resumeId, userId);
        return sections.Select(MapToDto).ToList();
    }

    public async Task<SectionDto?> GetSectionByIdAsync(int sectionId)
    {
        var section = await sectionRepo.FindBySectionIdAsync(sectionId);
        return section is null ? null : MapToDto(section);
    }

    public async Task<IList<SectionDto>> GetSectionsByTypeAsync(int resumeId, SectionType sectionType, int userId)
    {
        var sections = await sectionRepo.FindByResumeIdAndSectionTypeAsync(resumeId, sectionType, userId);
        return sections.Select(MapToDto).ToList();
    }

    public async Task<SectionDto> UpdateSectionAsync(int sectionId, int userId, UpdateSectionRequest request)
    {
        var section = await sectionRepo.FindBySectionIdAsync(sectionId)
                      ?? throw new KeyNotFoundException("Section not found.");
        
        if (section.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this section.");
            
        section.Title = request.Title;
        section.Content = request.Content;
        section.DisplayOrder = request.DisplayOrder;
        section.IsVisible = request.IsVisible;
        
        // If request explicitly sets AiGenerated, use it. 
        // Otherwise, if this is a manual update, set AiGenerated to false.
        section.AiGenerated = request.AiGenerated ?? false;

        var updated = await sectionRepo.UpdateAsync(section);
        return MapToDto(updated);
    }

    public Task DeleteSectionAsync(int sectionId, int userId)
        => sectionRepo.DeleteBySectionIdAsync(sectionId, userId);

    public Task DeleteAllSectionsAsync(int resumeId, int userId)
        => sectionRepo.DeleteByResumeIdAsync(resumeId, userId);

    public async Task ReorderSectionsAsync(int resumeId, int userId, ReorderSectionsRequest request)
    {
        // ExecuteUpdateAsync in a loop for atomic per-section order update
        for (int i = 0; i < request.OrderedSectionIds.Count; i++)
        {
            await sectionRepo.UpdateDisplayOrderAsync(request.OrderedSectionIds[i], userId, i);
        }
    }

    public async Task<SectionDto> ToggleVisibilityAsync(int sectionId, int userId)
    {
        var section = await sectionRepo.FindBySectionIdAsync(sectionId)
                      ?? throw new KeyNotFoundException("Section not found.");
        
        if (section.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this section.");
            
        section.IsVisible = !section.IsVisible;
        var updated = await sectionRepo.UpdateAsync(section);
        return MapToDto(updated);
    }

    public async Task<IList<SectionDto>> BulkUpdateSectionsAsync(int userId, BulkUpdateSectionsRequest request)
    {
        var sectionEntities = new List<ResumeSection>();
        foreach (var item in request.Sections)
        {
            var section = await sectionRepo.FindBySectionIdAsync(item.SectionId);
            if (section is null || section.UserId != userId) continue;

            section.Title = item.Title;
            section.Content = item.Content;
            section.DisplayOrder = item.DisplayOrder;
            section.IsVisible = item.IsVisible;
            section.AiGenerated = item.AiGenerated ?? false;

            sectionEntities.Add(section);
        }

        if (sectionEntities.Count > 0)
        {
            await sectionRepo.UpdateRangeAsync(sectionEntities);
        }

        return sectionEntities.Select(MapToDto).ToList();
    }

    public Task MarkAsAiGeneratedAsync(int sectionId, int userId)
        => sectionRepo.MarkAsAiGeneratedAsync(sectionId, userId);

    public Task<int> CountSectionsByResumeAsync(int resumeId, int userId)
        => sectionRepo.CountByResumeIdAsync(resumeId, userId);

    public async Task<SectionDto> CopySectionAsync(int sectionId, int userId, int targetResumeId)
    {
        var original = await sectionRepo.FindBySectionIdAsync(sectionId)
                       ?? throw new KeyNotFoundException("Section not found.");
        
        if (original.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this section.");
            
        var copy = new ResumeSection
        {
            ResumeId = targetResumeId,
            UserId = userId,
            SectionType = original.SectionType,
            Title = original.Title,
            Content = original.Content,
            DisplayOrder = original.DisplayOrder,
            IsVisible = original.IsVisible,
            AiGenerated = original.AiGenerated
        };
        
        var saved = await sectionRepo.AddAsync(copy);
        return MapToDto(saved);
    }

    private static SectionDto MapToDto(ResumeSection s) =>
        new(s.SectionId, s.ResumeId, s.SectionType, s.Title,
            s.Content, s.DisplayOrder, s.IsVisible, s.AiGenerated,
            s.CreatedAt, s.UpdatedAt);
}
