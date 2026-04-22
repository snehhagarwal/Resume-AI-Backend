using Microsoft.EntityFrameworkCore;
using ResumeAI.Template.API.Data;
using ResumeAI.Template.API.Entities;
using ResumeAI.Shared.Enums;
using ResumeAI.Template.API.Interfaces;

namespace ResumeAI.Template.API.Repositories;

public class TemplateRepository(TemplateDbContext db) : ITemplateRepository
{
    public Task<ResumeTemplate?> FindByTemplateIdAsync(int templateId)
        => db.ResumeTemplates.FindAsync(templateId).AsTask();

    public Task<IList<ResumeTemplate>> FindAllAsync()
        => db.ResumeTemplates.Where(t => t.IsActive).ToListAsync()
               .ContinueWith(t => (IList<ResumeTemplate>)t.Result);

    public Task<IList<ResumeTemplate>> FindByCategoryAsync(TemplateCategory category)
        => db.ResumeTemplates.Where(t => t.Category == category && t.IsActive)
               .ToListAsync()
               .ContinueWith(t => (IList<ResumeTemplate>)t.Result);

    public Task<IList<ResumeTemplate>> FindByIsPremiumAsync(bool isPremium)
        => db.ResumeTemplates.Where(t => t.IsPremium == isPremium && t.IsActive)
               .ToListAsync()
               .ContinueWith(t => (IList<ResumeTemplate>)t.Result);

    public Task<IList<ResumeTemplate>> FindByIsActiveAsync(bool isActive)
        => db.ResumeTemplates.Where(t => t.IsActive == isActive).ToListAsync()
               .ContinueWith(t => (IList<ResumeTemplate>)t.Result);

    public Task<IList<ResumeTemplate>> FindAllOrderByUsageCountDescAsync(int top = 10)
        => db.ResumeTemplates.Where(t => t.IsActive)
               .OrderByDescending(t => t.UsageCount).Take(top)
               .ToListAsync()
               .ContinueWith(t => (IList<ResumeTemplate>)t.Result);

    public Task<int> CountByCategoryAsync(TemplateCategory category)
        => db.ResumeTemplates.CountAsync(t => t.Category == category && t.IsActive);

    public async Task<ResumeTemplate> AddAsync(ResumeTemplate template)
    {
        db.ResumeTemplates.Add(template);
        await db.SaveChangesAsync();
        return template;
    }

    public async Task<ResumeTemplate> UpdateAsync(ResumeTemplate template)
    {
        db.ResumeTemplates.Update(template);
        await db.SaveChangesAsync();
        return template;
    }

    public Task UpdateUsageCountAsync(int templateId)
        => db.ResumeTemplates
             .Where(t => t.TemplateId == templateId)
             .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsageCount, t => t.UsageCount + 1));
}
