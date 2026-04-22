using ResumeAI.Section.API.Entities;
using ResumeAI.Section.API.Interfaces;
using ResumeAI.Section.API.Repositories;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Section.API.Services;

public class SectionService(ISectionRepository sectionRepo) : ISectionService
{
    public async Task<SectionDto> AddSectionAsync(AddSectionRequest request)
    {
        var section = new ResumeSection
        {
            ResumeId = request.ResumeId,
            SectionType = request.SectionType,
            Title = request.Title,
            Content = request.Content,
            DisplayOrder = request.DisplayOrder,
            IsVisible = request.IsVisible
        };
        var saved = await sectionRepo.AddAsync(section);
        return MapToDto(saved);
    }

    public async Task<IList<SectionDto>> GetSectionsByResumeAsync(int resumeId)
    {
        var sections = await sectionRepo.FindByResumeIdOrderByDisplayOrderAsync(resumeId);
        return sections.Select(MapToDto).ToList();
    }

    public async Task<SectionDto?> GetSectionByIdAsync(int sectionId)
    {
        var section = await sectionRepo.FindBySectionIdAsync(sectionId);
        return section is null ? null : MapToDto(section);
    }

    public async Task<IList<SectionDto>> GetSectionsByTypeAsync(int resumeId, SectionType sectionType)
    {
        var sections = await sectionRepo.FindByResumeIdAndSectionTypeAsync(resumeId, sectionType);
        return sections.Select(MapToDto).ToList();
    }

    public async Task<SectionDto> UpdateSectionAsync(int sectionId, UpdateSectionRequest request)
    {
        var section = await sectionRepo.FindBySectionIdAsync(sectionId)
                      ?? throw new KeyNotFoundException("Section not found.");
        
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

    public Task DeleteSectionAsync(int sectionId)
        => sectionRepo.DeleteBySectionIdAsync(sectionId);

    public Task DeleteAllSectionsAsync(int resumeId)
        => sectionRepo.DeleteByResumeIdAsync(resumeId);

    public async Task ReorderSectionsAsync(int resumeId, ReorderSectionsRequest request)
    {
        // ExecuteUpdateAsync in a loop for atomic per-section order update
        for (int i = 0; i < request.OrderedSectionIds.Count; i++)
        {
            await sectionRepo.UpdateDisplayOrderAsync(request.OrderedSectionIds[i], i);
        }
    }

    public async Task<SectionDto> ToggleVisibilityAsync(int sectionId)
    {
        var section = await sectionRepo.FindBySectionIdAsync(sectionId)
                      ?? throw new KeyNotFoundException("Section not found.");
        section.IsVisible = !section.IsVisible;
        var updated = await sectionRepo.UpdateAsync(section);
        return MapToDto(updated);
    }

    public async Task<IList<SectionDto>> BulkUpdateSectionsAsync(BulkUpdateSectionsRequest request)
    {
        var sectionEntities = new List<ResumeSection>();
        foreach (var item in request.Sections)
        {
            var section = await sectionRepo.FindBySectionIdAsync(item.SectionId);
            if (section is null) continue;

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

    public Task MarkAsAiGeneratedAsync(int sectionId)
        => sectionRepo.MarkAsAiGeneratedAsync(sectionId);

    public Task<int> CountSectionsByResumeAsync(int resumeId)
        => sectionRepo.CountByResumeIdAsync(resumeId);

    public async Task<SectionDto> CopySectionAsync(int sectionId, int targetResumeId)
    {
        var original = await sectionRepo.FindBySectionIdAsync(sectionId)
                       ?? throw new KeyNotFoundException("Section not found.");
        
        var copy = new ResumeSection
        {
            ResumeId = targetResumeId,
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
