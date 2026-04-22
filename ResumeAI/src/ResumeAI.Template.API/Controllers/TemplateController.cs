using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using ResumeAI.Template.API.Services;
using ResumeAI.Template.API.Interfaces;

namespace ResumeAI.Template.API.Controllers;

[ApiController]
[Route("api/templates")]
public class TemplateController(ITemplateService templateService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<IList<TemplateDto>>.Ok(await templateService.GetAllTemplatesAsync()));

    [AllowAnonymous]
    [HttpGet("free")]
    public async Task<IActionResult> GetFree()
        => Ok(ApiResponse<IList<TemplateDto>>.Ok(await templateService.GetFreeTemplatesAsync()));

    [Authorize]
    [HttpGet("premium")]
    public async Task<IActionResult> GetPremium()
        => Ok(ApiResponse<IList<TemplateDto>>.Ok(await templateService.GetPremiumTemplatesAsync()));

    [AllowAnonymous]
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(TemplateCategory category)
        => Ok(ApiResponse<IList<TemplateDto>>.Ok(await templateService.GetByCategoryAsync(category)));

    [AllowAnonymous]
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular([FromQuery] int top = 10)
        => Ok(ApiResponse<IList<TemplateDto>>.Ok(await templateService.GetPopularTemplatesAsync(top)));

    [AllowAnonymous]
    [HttpGet("{templateId:int}")]
    public async Task<IActionResult> GetById(int templateId)
    {
        var template = await templateService.GetTemplateByIdAsync(templateId);
        return template is null ? NotFound() : Ok(ApiResponse<TemplateDto>.Ok(template));
    }

    [AllowAnonymous]
    [HttpGet("{templateId:int}/preview")]
    public async Task<IActionResult> GetPreview(int templateId)
    {
        var preview = await templateService.GetTemplatePreviewAsync(templateId);
        return preview is null ? NotFound() : Ok(ApiResponse<TemplatePreviewDto>.Ok(preview));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request)
    {
        var (valid, error) = await templateService.ValidateTemplateLayoutAsync(request.HtmlLayout, request.CssStyles);
        if (!valid) return BadRequest(ApiResponse<TemplateDto>.Fail(error!));

        var template = await templateService.CreateTemplateAsync(request);
        return CreatedAtAction(nameof(GetById), new { templateId = template.TemplateId },
            ApiResponse<TemplateDto>.Ok(template));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{templateId:int}")]
    public async Task<IActionResult> Update(int templateId, [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            var (valid, error) = await templateService.ValidateTemplateLayoutAsync(request.HtmlLayout, request.CssStyles);
            if (!valid) return BadRequest(ApiResponse<TemplateDto>.Fail(error!));

            var template = await templateService.UpdateTemplateAsync(templateId, request);
            return Ok(ApiResponse<TemplateDto>.Ok(template));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<TemplateDto>.Fail(ex.Message)); }
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{templateId:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int templateId)
    {
        await templateService.DeactivateTemplateAsync(templateId);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{templateId:int}/increment-usage")]
    public async Task<IActionResult> IncrementUsage(int templateId)
    {
        await templateService.IncrementUsageAsync(templateId);
        return NoContent();
    }
}
