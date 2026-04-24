using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.AI.API.Interfaces;
using ResumeAI.Shared.DTOs;

namespace ResumeAI.AI.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController(IAiService aiService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());

    [HttpPost("generate-summary")]
    public async Task<IActionResult> GenerateSummary([FromBody] GenerateSummaryRequest request)
    {
        var result = await aiService.GenerateSummaryAsync(CurrentUserId, request);
        return Ok(ApiResponse<AiRequestDto>.Ok(result));
    }

    [HttpPost("generate-bullets")]
    public async Task<IActionResult> GenerateBullets([FromBody] GenerateBulletsRequest request)
    {
        var result = await aiService.GenerateBulletPointsAsync(CurrentUserId, request);
        return Ok(ApiResponse<AiRequestDto>.Ok(result));
    }

    [Authorize(Policy = "PremiumOnly")]
    [HttpPost("generate-cover-letter")]
    public async Task<IActionResult> GenerateCoverLetter([FromBody] GenerateCoverLetterRequest request)
    {
        var result = await aiService.GenerateCoverLetterAsync(CurrentUserId, request);
        return Ok(ApiResponse<AiRequestDto>.Ok(result));
    }

    [Authorize(Policy = "PremiumOnly")]
    [HttpPost("improve-section")]
    public async Task<IActionResult> ImproveSection([FromBody] ImproveSectionRequest request)
    {
        var result = await aiService.ImproveSectionAsync(CurrentUserId, request);
        return Ok(ApiResponse<AiRequestDto>.Ok(result));
    }

    [HttpPost("check-ats")]
    public async Task<IActionResult> CheckAts([FromBody] CheckAtsRequest request)
    {
        var result = await aiService.CheckAtsCompatibilityAsync(CurrentUserId, request);
        return Ok(ApiResponse<AiRequestDto>.Ok(result));
    }

    [HttpPost("suggest-skills")]
    public async Task<IActionResult> SuggestSkills([FromBody] SuggestSkillsRequest request)
    {
        var result = await aiService.SuggestSkillsAsync(CurrentUserId, request);
        return Ok(ApiResponse<AiRequestDto>.Ok(result));
    }

    [Authorize(Policy = "PremiumOnly")]
    [HttpPost("tailor-for-job")]
    public async Task<IActionResult> TailorForJob([FromBody] TailorResumeRequest request)
    {
        var result = await aiService.TailorResumeForJobAsync(CurrentUserId, request);
        return Ok(ApiResponse<AiRequestDto>.Ok(result));
    }

    [Authorize(Policy = "PremiumOnly")]
    [HttpPost("translate")]
    public async Task<IActionResult> Translate([FromBody] TranslateResumeRequest request)
    {
        var result = await aiService.TranslateResumeAsync(CurrentUserId, request);
        return Ok(ApiResponse<AiRequestDto>.Ok(result));
    }

    [Authorize(Policy = "PremiumOnly")]
    [HttpPost("analyze-job-fit")]
    public async Task<IActionResult> AnalyzeJobFit([FromBody] CheckAtsRequest request)
    {
        var result = await aiService.AnalyzeJobFitAsync(CurrentUserId, request);
        return Ok(ApiResponse<AiRequestDto>.Ok(result));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await aiService.GetAiHistoryAsync(CurrentUserId);
        return Ok(ApiResponse<IList<AiRequestDto>>.Ok(history));
    }

    [HttpGet("quota")]
    public async Task<IActionResult> GetQuota()
    {
        var quota = await aiService.GetRemainingQuotaAsync(CurrentUserId);
        return Ok(ApiResponse<AiQuotaDto>.Ok(quota));
    }
}
