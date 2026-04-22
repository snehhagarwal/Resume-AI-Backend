using ResumeAI.Shared.Enums;
using ResumeAI.Template.API.Entities;

namespace ResumeAI.Template.API.Interfaces;

public interface ITemplateRepository
{
    Task<ResumeTemplate?> FindByTemplateIdAsync(int templateId);
    Task<IList<ResumeTemplate>> FindAllAsync();
    Task<IList<ResumeTemplate>> FindByCategoryAsync(TemplateCategory category);
    Task<IList<ResumeTemplate>> FindByIsPremiumAsync(bool isPremium);
    Task<IList<ResumeTemplate>> FindByIsActiveAsync(bool isActive);
    Task<IList<ResumeTemplate>> FindAllOrderByUsageCountDescAsync(int top = 10);
    Task<int> CountByCategoryAsync(TemplateCategory category);
    Task<ResumeTemplate> AddAsync(ResumeTemplate template);
    Task<ResumeTemplate> UpdateAsync(ResumeTemplate template);
    Task UpdateUsageCountAsync(int templateId);
}
