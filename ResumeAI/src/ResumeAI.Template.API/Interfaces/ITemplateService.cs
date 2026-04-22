using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Template.API.Interfaces;

public interface ITemplateService
{
    Task<TemplateDto> CreateTemplateAsync(CreateTemplateRequest request);
    Task<TemplateDto?> GetTemplateByIdAsync(int templateId);
    Task<IList<TemplateDto>> GetAllTemplatesAsync();
    Task<IList<TemplateDto>> GetFreeTemplatesAsync();
    Task<IList<TemplateDto>> GetPremiumTemplatesAsync();
    Task<IList<TemplateDto>> GetByCategoryAsync(TemplateCategory category);
    Task<IList<TemplateDto>> GetPopularTemplatesAsync(int top = 10);
    Task<TemplateDto> UpdateTemplateAsync(int templateId, UpdateTemplateRequest request);
    Task DeactivateTemplateAsync(int templateId);
    Task IncrementUsageAsync(int templateId);
    Task<TemplatePreviewDto?> GetTemplatePreviewAsync(int templateId);
    Task<bool> CanUserAccessTemplateAsync(int templateId, SubscriptionPlan userPlan);
    Task<(bool Valid, string? Error)> ValidateTemplateLayoutAsync(string html, string css);
}
