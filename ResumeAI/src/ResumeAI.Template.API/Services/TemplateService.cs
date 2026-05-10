using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using ResumeAI.Template.API.Entities;
using ResumeAI.Template.API.Repositories;
using ResumeAI.Template.API.Interfaces;

namespace ResumeAI.Template.API.Services;

public class TemplateService(ITemplateRepository templateRepo) : ITemplateService
{
    public async Task<TemplateDto> CreateTemplateAsync(CreateTemplateRequest request)
    {
        var template = new ResumeTemplate
        {
            Name = request.Name,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            HtmlLayout = request.HtmlLayout,
            CssStyles = request.CssStyles,
            Category = request.Category,
            IsPremium = request.IsPremium
        };
        var saved = await templateRepo.AddAsync(template);
        return MapToDto(saved);
    }

    public async Task<TemplateDto?> GetTemplateByIdAsync(int templateId)
    {
        var t = await templateRepo.FindByTemplateIdAsync(templateId);
        return t is null ? null : MapToDto(t);
    }

    public async Task<IList<TemplateDto>> GetAllTemplatesAsync()
        => (await templateRepo.FindAllAsync()).Select(MapToDto).ToList();

    public async Task<IList<TemplateDto>> GetFreeTemplatesAsync()
        => (await templateRepo.FindByIsPremiumAsync(false)).Select(MapToDto).ToList();

    public async Task<IList<TemplateDto>> GetPremiumTemplatesAsync()
        => (await templateRepo.FindByIsPremiumAsync(true)).Select(MapToDto).ToList();

    public async Task<IList<TemplateDto>> GetByCategoryAsync(TemplateCategory category)
        => (await templateRepo.FindByCategoryAsync(category)).Select(MapToDto).ToList();

    public async Task<IList<TemplateDto>> GetPopularTemplatesAsync(int top = 10)
        => (await templateRepo.FindAllOrderByUsageCountDescAsync(top)).Select(MapToDto).ToList();

    public async Task<TemplateDto> UpdateTemplateAsync(int templateId, UpdateTemplateRequest request)
    {
        var template = await templateRepo.FindByTemplateIdAsync(templateId)
                       ?? throw new KeyNotFoundException("Template not found.");
        template.Name = request.Name;
        template.Description = request.Description;
        template.ThumbnailUrl = request.ThumbnailUrl;
        template.HtmlLayout = request.HtmlLayout;
        template.CssStyles = request.CssStyles;
        template.Category = request.Category;
        template.IsPremium = request.IsPremium;
        var updated = await templateRepo.UpdateAsync(template);
        return MapToDto(updated);
    }

    public async Task DeactivateTemplateAsync(int templateId)
    {
        var template = await templateRepo.FindByTemplateIdAsync(templateId)
                       ?? throw new KeyNotFoundException("Template not found.");
        template.IsActive = false;
        await templateRepo.UpdateAsync(template);
    }

    public Task IncrementUsageAsync(int templateId)
        => templateRepo.UpdateUsageCountAsync(templateId);

    public async Task<TemplatePreviewDto?> GetTemplatePreviewAsync(int templateId)
    {
        var t = await templateRepo.FindByTemplateIdAsync(templateId);
        return t is null ? null : new TemplatePreviewDto(t.TemplateId, t.HtmlLayout, t.CssStyles);
    }

    public async Task<bool> CanUserAccessTemplateAsync(int templateId, SubscriptionPlan userPlan)
    {
        var t = await templateRepo.FindByTemplateIdAsync(templateId);
        if (t == null) return false;
        if (!t.IsPremium) return true;
        return userPlan == SubscriptionPlan.PREMIUM;
    }

    public Task<(bool Valid, string? Error)> ValidateTemplateLayoutAsync(string html, string css)
    {
        if (string.IsNullOrWhiteSpace(html)) return Task.FromResult((false, (string?)"HTML layout cannot be empty."));
        if (!html.Contains("{{") || !html.Contains("}}")) 
            return Task.FromResult((false, (string?)"HTML layout must contain at least one placeholder (e.g. {{FullName}})."));
        
        // Simple sanitization check (could be more complex)
        if (html.Contains("<script") || html.Contains("javascript:"))
            return Task.FromResult((false, (string?)"HTML layout contains forbidden scripts."));

        return Task.FromResult((true, (string?)null));
    }

    private static TemplateDto MapToDto(ResumeTemplate t) =>
        new(t.TemplateId, t.Name, t.Description, t.ThumbnailUrl,
            t.HtmlLayout, t.CssStyles,
            t.Category, t.IsPremium, t.IsActive, t.UsageCount, t.CreatedAt);
}
