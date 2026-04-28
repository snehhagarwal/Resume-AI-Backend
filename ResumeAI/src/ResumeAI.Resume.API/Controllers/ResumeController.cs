using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Resume.API.Interfaces;
using ResumeAI.Resume.API.Services;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Resume.API.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize]
public class ResumeController(IResumeService resumeService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                  ?? User.FindFirst("sub")?.Value 
                  ?? throw new UnauthorizedAccessException());

    private SubscriptionPlan CurrentUserPlan =>
        Enum.TryParse<SubscriptionPlan>(User.FindFirstValue("plan"), out var plan)
            ? plan
            : SubscriptionPlan.FREE;

    [HttpPost]
    public async Task<IActionResult> CreateResume([FromBody] CreateResumeRequest request)
    {
        try
        {
            var resume = await resumeService.CreateResumeAsync(CurrentUserId, CurrentUserPlan, request);
            return CreatedAtAction(nameof(GetById), new { resumeId = resume.ResumeId },
                ApiResponse<ResumeDto>.Ok(resume));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ResumeDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{resumeId:int}/duplicate")]
    public async Task<IActionResult> Duplicate(int resumeId)
    {
        try
        {
            var copy = await resumeService.DuplicateResumeAsync(resumeId, CurrentUserId);
            return Ok(ApiResponse<ResumeDto>.Ok(copy));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ResumeDto>.Fail(ex.Message)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("{resumeId:int}")]
    public async Task<IActionResult> GetById(int resumeId)
    {
        var resume = await resumeService.GetResumeByIdAsync(resumeId);
        if (resume is null) return NotFound();
        
        // Ownership check: only owner can see private resumes
        if (!resume.IsPublic && resume.UserId != CurrentUserId)
            return Forbid();

        // Increment view count for public resumes viewed by others
        if (resume.IsPublic && resume.UserId != CurrentUserId)
            await resumeService.IncrementViewCountAsync(resumeId);
            
        return Ok(ApiResponse<ResumeDto>.Ok(resume));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyResumes()
    {
        var resumes = await resumeService.GetResumesByUserAsync(CurrentUserId);
        return Ok(ApiResponse<IList<ResumeDto>>.Ok(resumes));
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicGallery()
    {
        var resumes = await resumeService.GetPublicResumesAsync();
        return Ok(ApiResponse<IList<ResumeDto>>.Ok(resumes));
    }

    [HttpGet("by-template/{templateId:int}")]
    public async Task<IActionResult> GetByTemplate(int templateId)
    {
        var resumes = await resumeService.GetResumesByTemplateAsync(templateId);
        // Filter: only show the user's resumes for this template (unless we wanted to show all public ones, 
        // but typically this endpoint is used by the builder to show user's own resumes)
        var filtered = resumes.Where(r => r.UserId == CurrentUserId).ToList();
        return Ok(ApiResponse<IList<ResumeDto>>.Ok(filtered));
    }

    [HttpPut("{resumeId:int}")]
    public async Task<IActionResult> UpdateResume(int resumeId, [FromBody] UpdateResumeRequest request)
    {
        try
        {
            var resume = await resumeService.UpdateResumeAsync(resumeId, CurrentUserId, CurrentUserPlan, request);
            return Ok(ApiResponse<ResumeDto>.Ok(resume));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ResumeDto>.Fail(ex.Message)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{resumeId:int}/publish")]
    public async Task<IActionResult> Publish(int resumeId)
    {
        try
        {
            var resume = await resumeService.PublishResumeAsync(resumeId, CurrentUserId);
            return Ok(ApiResponse<ResumeDto>.Ok(resume));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ResumeDto>.Fail(ex.Message)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{resumeId:int}/unpublish")]
    public async Task<IActionResult> Unpublish(int resumeId)
    {
        try
        {
            var resume = await resumeService.UnpublishResumeAsync(resumeId, CurrentUserId);
            return Ok(ApiResponse<ResumeDto>.Ok(resume));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ResumeDto>.Fail(ex.Message)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{resumeId:int}/ats-score")]
    public async Task<IActionResult> UpdateAtsScore(int resumeId, [FromBody] UpdateAtsScoreRequest request)
    {
        var resume = await resumeService.GetResumeByIdAsync(resumeId);
        if (resume == null) return NotFound();
        if (resume.UserId != CurrentUserId) return Forbid();

        await resumeService.UpdateAtsScoreAsync(resumeId, request.Score);
        return NoContent();
    }

    [HttpDelete("{resumeId:int}")]
    public async Task<IActionResult> DeleteResume(int resumeId)
    {
        try
        {
            await resumeService.DeleteResumeAsync(resumeId, CurrentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ResumeDto>.Fail(ex.Message)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
