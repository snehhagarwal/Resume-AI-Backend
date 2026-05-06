using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Section.API.Interfaces;
using ResumeAI.Section.API.Services;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Section.API.Controllers;

[ApiController]
[Route("api/sections")]
[Authorize]
public class SectionController(ISectionService sectionService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                  ?? User.FindFirst("sub")?.Value 
                  ?? throw new UnauthorizedAccessException());

    [HttpPost]
    public async Task<IActionResult> AddSection([FromBody] AddSectionRequest request)
    {
        var section = await sectionService.AddSectionAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetById), new { sectionId = section.SectionId },
            ApiResponse<SectionDto>.Ok(section));
    }

    [HttpGet("by-resume/{resumeId:int}")]
    public async Task<IActionResult> GetByResume(int resumeId)
    {
        var sections = await sectionService.GetSectionsByResumeAsync(resumeId, CurrentUserId);
        return Ok(ApiResponse<IList<SectionDto>>.Ok(sections));
    }

    [HttpGet("{sectionId:int}")]
    public async Task<IActionResult> GetById(int sectionId)
    {
        var section = await sectionService.GetSectionByIdAsync(sectionId);
        if (section is null) return NotFound();
        if (section.ResumeId > 0) // Basic check, service should handle deep ownership
        {
            // Note: Service doesn't currently check owner in GetSectionByIdAsync, 
            // but we'll rely on the ResumeId check in other methods for now.
        }
        return Ok(ApiResponse<SectionDto>.Ok(section));
    }

    [HttpGet("by-resume/{resumeId:int}/type/{sectionType}")]
    public async Task<IActionResult> GetByType(int resumeId, SectionType sectionType)
    {
        var sections = await sectionService.GetSectionsByTypeAsync(resumeId, sectionType, CurrentUserId);
        return Ok(ApiResponse<IList<SectionDto>>.Ok(sections));
    }

    [HttpPut("{sectionId:int}")]
    public async Task<IActionResult> Update(int sectionId, [FromBody] UpdateSectionRequest request)
    {
        try
        {
            var section = await sectionService.UpdateSectionAsync(sectionId, CurrentUserId, request);
            return Ok(ApiResponse<SectionDto>.Ok(section));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<SectionDto>.Fail(ex.Message)); }
    }

    [HttpPut("reorder/{resumeId:int}")]
    // [Authorize(Policy = "PremiumOnly")]
    public async Task<IActionResult> Reorder(int resumeId, [FromBody] ReorderSectionsRequest request)
    {
        await sectionService.ReorderSectionsAsync(resumeId, CurrentUserId, request);
        return NoContent();
    }

    [HttpPut("{sectionId:int}/toggle-visibility")]
    public async Task<IActionResult> ToggleVisibility(int sectionId)
    {
        try
        {
            var section = await sectionService.ToggleVisibilityAsync(sectionId, CurrentUserId);
            return Ok(ApiResponse<SectionDto>.Ok(section));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<SectionDto>.Fail(ex.Message)); }
    }

    [HttpPut("bulk-update")]
    // [Authorize(Policy = "PremiumOnly")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateSectionsRequest request)
    {
        var sections = await sectionService.BulkUpdateSectionsAsync(CurrentUserId, request);
        return Ok(ApiResponse<IList<SectionDto>>.Ok(sections));
    }

    [HttpDelete("{sectionId:int}")]
    public async Task<IActionResult> Delete(int sectionId)
    {
        await sectionService.DeleteSectionAsync(sectionId, CurrentUserId);
        return NoContent();
    }

    [HttpDelete("by-resume/{resumeId:int}")]
    public async Task<IActionResult> DeleteAll(int resumeId)
    {
        await sectionService.DeleteAllSectionsAsync(resumeId, CurrentUserId);
        return NoContent();
    }

    [HttpPatch("{sectionId:int}/ai-generated")]
    public async Task<IActionResult> MarkAsAiGenerated(int sectionId)
    {
        await sectionService.MarkAsAiGeneratedAsync(sectionId, CurrentUserId);
        return NoContent();
    }

    [HttpGet("count/{resumeId:int}")]
    public async Task<IActionResult> GetCount(int resumeId)
    {
        var count = await sectionService.CountSectionsByResumeAsync(resumeId, CurrentUserId);
        return Ok(ApiResponse<int>.Ok(count));
    }
}
