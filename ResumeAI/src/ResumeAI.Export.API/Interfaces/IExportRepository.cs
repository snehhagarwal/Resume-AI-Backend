using ResumeAI.Export.API.Entities;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Export.API.Interfaces;

public interface IExportRepository
{
    Task<ExportJob?> FindByJobIdAsync(string jobId);
    Task<IList<ExportJob>> FindByUserIdAsync(int userId);
    Task<IList<ExportJob>> FindByResumeIdAsync(int resumeId);
    Task<IList<ExportJob>> FindByStatusAsync(ExportStatus status);
    Task<IList<ExportJob>> FindByFormatAsync(ExportFormat format);
    Task<IList<ExportJob>> FindExpiredJobsAsync(DateTime before);
    Task<int> CountByUserIdTodayAsync(int userId);
    Task<ExportJob> AddAsync(ExportJob job);
    Task<ExportJob> UpdateAsync(ExportJob job);
    Task DeleteByJobIdAsync(string jobId);
    Task DeleteExpiredJobsAsync(DateTime before);
}
