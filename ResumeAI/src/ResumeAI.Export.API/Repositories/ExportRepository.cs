using Microsoft.EntityFrameworkCore;
using ResumeAI.Export.API.Data;
using ResumeAI.Export.API.Entities;
using ResumeAI.Export.API.Interfaces;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Export.API.Repositories;

public class ExportRepository(ExportDbContext db) : IExportRepository
{
    public Task<ExportJob?> FindByJobIdAsync(string jobId)
        => db.ExportJobs.FindAsync(jobId).AsTask();

    public Task<IList<ExportJob>> FindByUserIdAsync(int userId)
        => db.ExportJobs.Where(j => j.UserId == userId)
               .OrderByDescending(j => j.RequestedAt).ToListAsync()
               .ContinueWith(t => (IList<ExportJob>)t.Result);

    public Task<IList<ExportJob>> FindByResumeIdAsync(int resumeId)
        => db.ExportJobs.Where(j => j.ResumeId == resumeId).ToListAsync()
               .ContinueWith(t => (IList<ExportJob>)t.Result);

    public Task<IList<ExportJob>> FindByStatusAsync(ExportStatus status)
        => db.ExportJobs.Where(j => j.Status == status).ToListAsync()
               .ContinueWith(t => (IList<ExportJob>)t.Result);

    public Task<IList<ExportJob>> FindByFormatAsync(ExportFormat format)
        => db.ExportJobs.Where(j => j.Format == format).ToListAsync()
               .ContinueWith(t => (IList<ExportJob>)t.Result);

    public Task<IList<ExportJob>> FindExpiredJobsAsync(DateTime before)
        => db.ExportJobs.Where(j => j.ExpiresAt < before).ToListAsync()
               .ContinueWith(t => (IList<ExportJob>)t.Result);

    public Task<int> CountByUserIdTodayAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.ExportJobs.CountAsync(j =>
            j.UserId == userId &&
            DateOnly.FromDateTime(j.RequestedAt) == today);
    }

    public async Task<ExportJob> AddAsync(ExportJob job)
    {
        db.ExportJobs.Add(job);
        await db.SaveChangesAsync();
        return job;
    }

    public async Task<ExportJob> UpdateAsync(ExportJob job)
    {
        db.ExportJobs.Update(job);
        await db.SaveChangesAsync();
        return job;
    }

    public Task DeleteByJobIdAsync(string jobId)
        => db.ExportJobs.Where(j => j.JobId == jobId).ExecuteDeleteAsync();

    public Task DeleteExpiredJobsAsync(DateTime before)
        => db.ExportJobs.Where(j => j.ExpiresAt < before).ExecuteDeleteAsync();
}
