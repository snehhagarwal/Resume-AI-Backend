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
    [HttpPost]
    public async Task<IActionResult> AddSection([FromBody] AddSectionRequest request)
    {
        var section = await sectionService.AddSectionAsync(request);
        return CreatedAtAction(nameof(GetById), new { sectionId = section.SectionId },
            ApiResponse<SectionDto>.Ok(section));
    }

    [HttpGet("by-resume/{resumeId:int}")]
    public async Task<IActionResult> GetByResume(int resumeId)
    {
        var sections = await sectionService.GetSectionsByResumeAsync(resumeId);
        return Ok(ApiResponse<IList<SectionDto>>.Ok(sections));
    }

    [HttpGet("{sectionId:int}")]
    public async Task<IActionResult> GetById(int sectionId)
    {
        var section = await sectionService.GetSectionByIdAsync(sectionId);
        return section is null ? NotFound() : Ok(ApiResponse<SectionDto>.Ok(section));
    }

    [HttpGet("by-resume/{resumeId:int}/type/{sectionType}")]
    public async Task<IActionResult> GetByType(int resumeId, SectionType sectionType)
    {
        var sections = await sectionService.GetSectionsByTypeAsync(resumeId, sectionType);
        return Ok(ApiResponse<IList<SectionDto>>.Ok(sections));
    }

    [HttpPut("{sectionId:int}")]
    public async Task<IActionResult> Update(int sectionId, [FromBody] UpdateSectionRequest request)
    {
        try
        {
            var section = await sectionService.UpdateSectionAsync(sectionId, request);
            return Ok(ApiResponse<SectionDto>.Ok(section));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<SectionDto>.Fail(ex.Message)); }
    }

    [HttpPut("reorder/{resumeId:int}")]
    // [Authorize(Policy = "PremiumOnly")]
    public async Task<IActionResult> Reorder(int resumeId, [FromBody] ReorderSectionsRequest request)
    {
        await sectionService.ReorderSectionsAsync(resumeId, request);
        return NoContent();
    }

    [HttpPut("{sectionId:int}/toggle-visibility")]
    public async Task<IActionResult> ToggleVisibility(int sectionId)
    {
        try
        {
            var section = await sectionService.ToggleVisibilityAsync(sectionId);
            return Ok(ApiResponse<SectionDto>.Ok(section));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<SectionDto>.Fail(ex.Message)); }
    }

    [HttpPut("bulk-update")]
    // [Authorize(Policy = "PremiumOnly")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateSectionsRequest request)
    {
        var sections = await sectionService.BulkUpdateSectionsAsync(request);
        return Ok(ApiResponse<IList<SectionDto>>.Ok(sections));
    }

    [HttpDelete("{sectionId:int}")]
    public async Task<IActionResult> Delete(int sectionId)
    {
        await sectionService.DeleteSectionAsync(sectionId);
        return NoContent();
    }

    [HttpDelete("by-resume/{resumeId:int}")]
    public async Task<IActionResult> DeleteAll(int resumeId)
    {
        await sectionService.DeleteAllSectionsAsync(resumeId);
        return NoContent();
    }

    [HttpPatch("{sectionId:int}/ai-generated")]
    public async Task<IActionResult> MarkAsAiGenerated(int sectionId)
    {
        await sectionService.MarkAsAiGeneratedAsync(sectionId);
        return NoContent();
    }

    [HttpGet("count/{resumeId:int}")]
    public async Task<IActionResult> GetCount(int resumeId)
    {
        var count = await sectionService.CountSectionsByResumeAsync(resumeId);
        return Ok(ApiResponse<int>.Ok(count));
    }
}
