using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Export.API.Interfaces;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Export.API.Controllers;

[ApiController]
[Route("api/exports")]
[Authorize]
public class ExportController(IExportService exportService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());

    [HttpPost("pdf")]
    public async Task<IActionResult> ExportPdf([FromBody] ExportRequest request)
    {
        var job = await exportService.ExportToPdfAsync(CurrentUserId, request);
        return Ok(ApiResponse<ExportJobDto>.Ok(job));
    }

    [Authorize(Policy = "PremiumOnly")]
    [HttpPost("docx")]
    public async Task<IActionResult> ExportDocx([FromBody] ExportRequest request)
    {
        var job = await exportService.ExportToDocxAsync(CurrentUserId, request);
        return Ok(ApiResponse<ExportJobDto>.Ok(job));
    }

    [Authorize(Policy = "PremiumOnly")]
    [HttpPost("json")]
    public async Task<IActionResult> ExportJson([FromBody] ExportRequest request)
    {
        var job = await exportService.ExportToJsonAsync(CurrentUserId, request);
        return Ok(ApiResponse<ExportJobDto>.Ok(job));
    }

    [HttpGet("{jobId}/status")]
    public async Task<IActionResult> GetStatus(string jobId)
    {
        var job = await exportService.GetJobStatusAsync(jobId);
        return job is null ? NotFound() : Ok(ApiResponse<ExportJobDto>.Ok(job));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyExports()
    {
        var jobs = await exportService.GetExportsByUserAsync(CurrentUserId);
        return Ok(ApiResponse<IList<ExportJobDto>>.Ok(jobs));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await exportService.GetExportStatsAsync();
        return Ok(ApiResponse<IDictionary<string, int>>.Ok(stats));
    }

    [HttpGet("{jobId}/download")]
    public async Task<IActionResult> Download(string jobId)
    {
        try
        {
            var job = await exportService.GetJobStatusAsync(jobId);
            if (job == null) return NotFound();

            var bytes = await exportService.DownloadFileAsync(jobId);
            
            var contentType = job.Format switch
            {
                ExportFormat.PDF => "application/pdf",
                ExportFormat.DOCX => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ExportFormat.JSON => "application/json",
                _ => "application/octet-stream"
            };

            var extension = job.Format switch
            {
                ExportFormat.PDF => ".pdf",
                ExportFormat.DOCX => ".docx",
                ExportFormat.JSON => ".json",
                _ => ".bin"
            };

            return File(bytes, contentType, $"resume_{job.ResumeId}{extension}");
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("{jobId}")]
    public async Task<IActionResult> Delete(string jobId)
    {
        await exportService.DeleteExportAsync(jobId);
        return NoContent();
    }
}
