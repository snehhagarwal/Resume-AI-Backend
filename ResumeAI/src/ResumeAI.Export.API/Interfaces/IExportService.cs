using ResumeAI.Shared.DTOs;

namespace ResumeAI.Export.API.Interfaces;

public interface IExportService
{
    Task<ExportJobDto> ExportToPdfAsync(int userId, ExportRequest request);
    Task<ExportJobDto> ExportToDocxAsync(int userId, ExportRequest request);
    Task<ExportJobDto> ExportToJsonAsync(int userId, ExportRequest request);
    Task<ExportJobDto?> GetJobStatusAsync(string jobId);
    Task<IList<ExportJobDto>> GetExportsByUserAsync(int userId);
    Task<byte[]> DownloadFileAsync(string jobId);
    Task DeleteExportAsync(string jobId);
    Task CleanupExpiredExportsAsync();
    Task<IDictionary<string, int>> GetExportStatsAsync();
}
