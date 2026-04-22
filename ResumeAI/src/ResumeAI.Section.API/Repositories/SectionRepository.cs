using Microsoft.EntityFrameworkCore;
using ResumeAI.Section.API.Data;
using ResumeAI.Section.API.Entities;
using ResumeAI.Section.API.Interfaces;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Section.API.Repositories;

public class SectionRepository(SectionDbContext db) : ISectionRepository
{
    public Task<IList<ResumeSection>> FindByResumeIdAsync(int resumeId)
        => db.ResumeSections.Where(s => s.ResumeId == resumeId)
               .OrderBy(s => s.DisplayOrder).ToListAsync()
               .ContinueWith(t => (IList<ResumeSection>)t.Result);

    public Task<IList<ResumeSection>> FindByResumeIdAndSectionTypeAsync(int resumeId, SectionType sectionType)
        => db.ResumeSections
               .Where(s => s.ResumeId == resumeId && s.SectionType == sectionType)
               .ToListAsync()
               .ContinueWith(t => (IList<ResumeSection>)t.Result);

    public Task<ResumeSection?> FindBySectionIdAsync(int sectionId)
        => db.ResumeSections.FindAsync(sectionId).AsTask();

    public Task<IList<ResumeSection>> FindByResumeIdOrderByDisplayOrderAsync(int resumeId)
        => db.ResumeSections.Where(s => s.ResumeId == resumeId)
               .OrderBy(s => s.DisplayOrder).ToListAsync()
               .ContinueWith(t => (IList<ResumeSection>)t.Result);

    public Task<IList<ResumeSection>> FindByAiGeneratedAsync(bool aiGenerated)
        => db.ResumeSections.Where(s => s.AiGenerated == aiGenerated)
               .ToListAsync()
               .ContinueWith(t => (IList<ResumeSection>)t.Result);

    public Task<int> CountByResumeIdAsync(int resumeId)
        => db.ResumeSections.CountAsync(s => s.ResumeId == resumeId);

    public async Task<ResumeSection> AddAsync(ResumeSection section)
    {
        db.ResumeSections.Add(section);
        await db.SaveChangesAsync();
        return section;
    }

    public async Task<ResumeSection> UpdateAsync(ResumeSection section)
    {
        section.UpdatedAt = DateTime.UtcNow;
        db.ResumeSections.Update(section);
        await db.SaveChangesAsync();
        return section;
    }

    public Task UpdateDisplayOrderAsync(int sectionId, int displayOrder)
        => db.ResumeSections
             .Where(s => s.SectionId == sectionId)
             .ExecuteUpdateAsync(p => p.SetProperty(s => s.DisplayOrder, displayOrder));

    public Task DeleteByResumeIdAsync(int resumeId)
        => db.ResumeSections.Where(s => s.ResumeId == resumeId).ExecuteDeleteAsync();

    public Task DeleteBySectionIdAsync(int sectionId)
        => db.ResumeSections.Where(s => s.SectionId == sectionId).ExecuteDeleteAsync();

    public Task MarkAsAiGeneratedAsync(int sectionId)
        => db.ResumeSections
             .Where(s => s.SectionId == sectionId)
             .ExecuteUpdateAsync(p => p.SetProperty(s => s.AiGenerated, true));

    public async Task UpdateRangeAsync(IEnumerable<ResumeSection> sections)
    {
        foreach (var section in sections)
        {
            section.UpdatedAt = DateTime.UtcNow;
        }
        db.ResumeSections.UpdateRange(sections);
        await db.SaveChangesAsync();
    }
}
