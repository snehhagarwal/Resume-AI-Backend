using Microsoft.EntityFrameworkCore;
using ResumeAI.Resume.API.Data;
using ResumeAI.Resume.API.Entities;
using ResumeAI.Resume.API.Interfaces;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Resume.API.Repositories;

public class ResumeRepository(ResumeDbContext db) : IResumeRepository
{
    public Task<ResumeRecord?> FindByResumeIdAsync(int resumeId)
        => db.Resumes.FindAsync(resumeId).AsTask();

    public Task<IList<ResumeRecord>> FindByUserIdAsync(int userId)
        => db.Resumes.Where(r => r.UserId == userId)
               .OrderByDescending(r => r.UpdatedAt)
               .ToListAsync()
               .ContinueWith(t => (IList<ResumeRecord>)t.Result);

    public Task<IList<ResumeRecord>> FindByStatusAsync(ResumeStatus status)
        => db.Resumes.Where(r => r.Status == status).ToListAsync()
               .ContinueWith(t => (IList<ResumeRecord>)t.Result);

    public Task<IList<ResumeRecord>> FindByTargetJobTitleAsync(string targetJobTitle)
        => db.Resumes.Where(r => r.TargetJobTitle.Contains(targetJobTitle))
               .ToListAsync()
               .ContinueWith(t => (IList<ResumeRecord>)t.Result);

    public Task<IList<ResumeRecord>> FindByIsPublicAsync(bool isPublic)
        => db.Resumes.Where(r => r.IsPublic == isPublic)
               .OrderByDescending(r => r.ViewCount)
               .ToListAsync()
               .ContinueWith(t => (IList<ResumeRecord>)t.Result);

    public Task<int> CountByUserIdAsync(int userId)
        => db.Resumes.CountAsync(r => r.UserId == userId);

    public Task<IList<ResumeRecord>> FindByTemplateIdAsync(int templateId)
        => db.Resumes.Where(r => r.TemplateId == templateId).ToListAsync()
               .ContinueWith(t => (IList<ResumeRecord>)t.Result);

    public async Task<ResumeRecord> AddAsync(ResumeRecord resume)
    {
        db.Resumes.Add(resume);
        await db.SaveChangesAsync();
        return resume;
    }

    public async Task<ResumeRecord> UpdateAsync(ResumeRecord resume)
    {
        resume.UpdatedAt = DateTime.UtcNow;
        db.Resumes.Update(resume);
        await db.SaveChangesAsync();
        return resume;
    }

    public Task UpdateAtsScoreAsync(int resumeId, int score)
        => db.Resumes
             .Where(r => r.ResumeId == resumeId)
             .ExecuteUpdateAsync(s => s.SetProperty(r => r.AtsScore, score));

    public Task IncrementViewCountAsync(int resumeId)
        => db.Resumes
             .Where(r => r.ResumeId == resumeId)
             .ExecuteUpdateAsync(s => s.SetProperty(r => r.ViewCount, r => r.ViewCount + 1));

    public Task DeleteByResumeIdAsync(int resumeId)
        => db.Resumes.Where(r => r.ResumeId == resumeId).ExecuteDeleteAsync();

    public Task<ResumeRecord?> FindWithSectionsAsync(int resumeId)
        => db.Resumes
             .Include(r => r.Sections)
             .AsNoTracking()
             .FirstOrDefaultAsync(r => r.ResumeId == resumeId);
}
