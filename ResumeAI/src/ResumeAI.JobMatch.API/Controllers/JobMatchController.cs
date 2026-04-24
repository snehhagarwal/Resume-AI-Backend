using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.JobMatch.API.Interfaces;
using ResumeAI.Shared.DTOs;

namespace ResumeAI.JobMatch.API.Controllers;

[ApiController]
[Route("api/job-matches")]
[Authorize(Policy = "PremiumOnly")]
public class JobMatchController(IJobMatchService matchService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeJobFitRequest request)
    {
        var match = await matchService.AnalyzeJobFit(CurrentUserId, request);
        return Ok(ApiResponse<JobMatchDto>.Ok(match));
    }

    [HttpGet("by-resume/{resumeId:int}")]
    public async Task<IActionResult> GetByResume(int resumeId)
        => Ok(ApiResponse<IList<JobMatchDto>>.Ok(await matchService.GetMatchesByResume(resumeId)));

    [HttpGet("my")]
    public async Task<IActionResult> GetByUser()
        => Ok(ApiResponse<IList<JobMatchDto>>.Ok(await matchService.GetMatchesByUser(CurrentUserId)));

    [HttpGet("{matchId:int}")]
    public async Task<IActionResult> GetById(int matchId)
    {
        var match = await matchService.GetMatchById(matchId);
        return match is null ? NotFound() : Ok(ApiResponse<JobMatchDto>.Ok(match));
    }

    [HttpGet("top")]
    public async Task<IActionResult> GetTopMatches([FromQuery] int minScore = 70)
        => Ok(ApiResponse<IList<JobMatchDto>>.Ok(await matchService.GetTopMatches(CurrentUserId, minScore)));

    [HttpPost("{matchId:int}/bookmark")]
    public async Task<IActionResult> Bookmark(int matchId, [FromQuery] bool bookmarked = true)
    {
        await matchService.BookmarkMatch(matchId, bookmarked);
        return NoContent();
    }

    [HttpPost("fetch/linkedin")]
    public async Task<IActionResult> FetchLinkedIn([FromQuery] int resumeId, [FromQuery] string keywords)
    {
        var matches = await matchService.FetchJobsFromLinkedIn(CurrentUserId, resumeId, keywords);
        return Ok(ApiResponse<IList<JobMatchDto>>.Ok(matches));
    }

    [HttpGet("{matchId:int}/recommendations")]
    public async Task<IActionResult> GetRecommendations(int matchId)
    {
        try
        {
            var rec = await matchService.GetTailoringRecommendations(matchId);
            return Ok(ApiResponse<string>.Ok(rec));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<string>.Fail(ex.Message)); }
    }

    [HttpDelete("{matchId:int}")]
    public async Task<IActionResult> Delete(int matchId)
    {
        await matchService.DeleteMatch(matchId);
        return NoContent();
    }
}
