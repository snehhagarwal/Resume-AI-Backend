using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using ResumeAI.Export.API.Entities;
using ResumeAI.Export.API.Interfaces;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace ResumeAI.Export.API.Services;

/// <summary>
/// Export service — PDF via QuestPDF, DOCX via OpenXML SDK,
/// JSON via System.Text.Json. Files stored in Azure Blob Storage
/// or Local Storage fallback.
/// </summary>
public class ExportService(
    IExportRepository exportRepo,
    IConfiguration config,
    IPdfRenderer pdfRenderer,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ExportService> logger) : IExportService
{
    public async Task<ExportJobDto> ExportToPdfAsync(int userId, ExportRequest request)
    {
        var job = await CreateJobAsync(userId, request, ExportFormat.PDF);

        try
        {
            job.Status = ExportStatus.PROCESSING;
            await exportRepo.UpdateAsync(job);

            var exportData = await GatherExportDataAsync(request.ResumeId);
            var pdfBytes = pdfRenderer.GeneratePdf(exportData);
            var url = await UploadToBlobAsync(job.JobId, pdfBytes, "application/pdf");

            job.FileUrl = url;
            job.FileSizeKb = pdfBytes.Length / 1024;
            job.Status = ExportStatus.COMPLETED;
            job.CompletedAt = DateTime.UtcNow;
            job.ExpiresAt = DateTime.UtcNow.AddDays(7);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PDF export failed for job {JobId}. Error: {Message}", job.JobId, ex.Message);
            job.Status = ExportStatus.FAILED;
        }

        var updated = await exportRepo.UpdateAsync(job);
        return MapToDto(updated);
    }

    public async Task<ExportJobDto> ExportToDocxAsync(int userId, ExportRequest request)
    {
        var job = await CreateJobAsync(userId, request, ExportFormat.DOCX);

        try
        {
            job.Status = ExportStatus.PROCESSING;
            await exportRepo.UpdateAsync(job);

            var exportData = await GatherExportDataAsync(request.ResumeId);
            var docxBytes = GenerateDocx(exportData);
            var url = await UploadToBlobAsync(job.JobId, docxBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            job.FileUrl = url;
            job.FileSizeKb = docxBytes.Length / 1024;
            job.Status = ExportStatus.COMPLETED;
            job.CompletedAt = DateTime.UtcNow;
            job.ExpiresAt = DateTime.UtcNow.AddDays(7);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DOCX export failed for job {JobId}. Error: {Message}", job.JobId, ex.Message);
            job.Status = ExportStatus.FAILED;
        }

        var updated = await exportRepo.UpdateAsync(job);
        return MapToDto(updated);
    }

    public async Task<ExportJobDto> ExportToJsonAsync(int userId, ExportRequest request)
    {
        var job = await CreateJobAsync(userId, request, ExportFormat.JSON);

        try
        {
            job.Status = ExportStatus.PROCESSING;
            await exportRepo.UpdateAsync(job);

            var exportData = await GatherExportDataAsync(request.ResumeId);
            var payload = JsonSerializer.SerializeToUtf8Bytes(exportData);
            var url = await UploadToBlobAsync(job.JobId, payload, "application/json");

            job.FileUrl = url;
            job.FileSizeKb = payload.Length / 1024;
            job.Status = ExportStatus.COMPLETED;
            job.CompletedAt = DateTime.UtcNow;
            job.ExpiresAt = DateTime.UtcNow.AddDays(7);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "JSON export failed for job {JobId}. Error: {Message}", job.JobId, ex.Message);
            job.Status = ExportStatus.FAILED;
        }

        var updated = await exportRepo.UpdateAsync(job);
        return MapToDto(updated);
    }

    public async Task<ExportJobDto?> GetJobStatusAsync(string jobId)
    {
        var job = await exportRepo.FindByJobIdAsync(jobId);
        return job is null ? null : MapToDto(job);
    }

    public async Task<IList<ExportJobDto>> GetExportsByUserAsync(int userId)
        => (await exportRepo.FindByUserIdAsync(userId)).Select(MapToDto).ToList();

    public async Task<byte[]> DownloadFileAsync(string jobId)
    {
        var job = await exportRepo.FindByJobIdAsync(jobId)
                  ?? throw new KeyNotFoundException("Export job not found.");
        if (job.Status != ExportStatus.COMPLETED || job.FileUrl is null)
            throw new InvalidOperationException("Export not ready.");

        // If it's a local file URL
        if (job.FileUrl.StartsWith("local://"))
        {
            var fileName = job.FileUrl.Replace("local://", "");
            var path = Path.Combine(Directory.GetCurrentDirectory(), "exports", fileName);
            if (File.Exists(path))
            {
                return await File.ReadAllBytesAsync(path);
            }
            logger.LogError("Local file not found at {Path}", path);
            throw new FileNotFoundException("Local export file missing.");
        }

        var blobClient = new BlobClient(new Uri(job.FileUrl));
        var download = await blobClient.DownloadContentAsync();
        return download.Value.Content.ToArray();
    }

    public Task DeleteExportAsync(string jobId)
        => exportRepo.DeleteByJobIdAsync(jobId);

    public Task CleanupExpiredExportsAsync()
        => exportRepo.DeleteExpiredJobsAsync(DateTime.UtcNow);

    public async Task<IDictionary<string, int>> GetExportStatsAsync()
    {
        var stats = new Dictionary<string, int>();
        foreach (ExportStatus status in Enum.GetValues(typeof(ExportStatus)))
        {
            var jobs = await exportRepo.FindByStatusAsync(status);
            stats[status.ToString()] = jobs.Count;
        }
        return stats;
    }

    private async Task<ExportData> GatherExportDataAsync(int resumeId)
    {
        var resume = await FetchResumeAsync(resumeId);
        var sections = await FetchSectionsAsync(resumeId);
        var user = await FetchUserAsync();
        
        return new ExportData(resume, user, sections);
    }

    private async Task<ResumeDto> FetchResumeAsync(int resumeId)
    {
        var client = httpClientFactory.CreateClient();
        var baseUrl = config["Services:ResumeApi"] ?? "http://localhost:5002";
        
        ForwardToken(client);

        var response = await client.GetAsync($"{baseUrl}/api/resumes/{resumeId}");
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ResumeDto>>(GetJsonOptions());
        return apiResponse?.Data ?? throw new InvalidOperationException("Failed to fetch resume data.");
    }

    private async Task<IList<SectionDto>> FetchSectionsAsync(int resumeId)
    {
        var client = httpClientFactory.CreateClient();
        var baseUrl = config["Services:SectionApi"] ?? "http://localhost:5003";
        
        ForwardToken(client);

        var response = await client.GetAsync($"{baseUrl}/api/sections/by-resume/{resumeId}");
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<IList<SectionDto>>>(GetJsonOptions());
        return apiResponse?.Data ?? new List<SectionDto>();
    }

    private async Task<UserDto> FetchUserAsync()
    {
        var client = httpClientFactory.CreateClient();
        var baseUrl = config["Services:AuthApi"] ?? "http://localhost:5001";
        
        ForwardToken(client);

        var response = await client.GetAsync($"{baseUrl}/api/auth/profile");
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>(GetJsonOptions());
        return apiResponse?.Data ?? throw new InvalidOperationException("Failed to fetch user profile.");
    }

    private void ForwardToken(HttpClient client)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader))
        {
            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader);
        }
    }

    private JsonSerializerOptions GetJsonOptions() => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // ─── DOCX Generation (OpenXML SDK) ───────────────────────────

    private static byte[] GenerateDocx(ExportData data)
    {
        var resume = data.Resume;
        var user = data.User;
        var sections = data.Sections;

        using var ms = new MemoryStream();
        using var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(
            new Body(
                new Paragraph(
                    new Run(
                        new RunProperties(new Bold(), new FontSize { Val = "48" }),
                        new Text(user.FullName))),
                new Paragraph(
                    new Run(
                        new RunProperties(new Italic(), new FontSize { Val = "28" }),
                        new Text(resume.TargetJobTitle))),
                new Paragraph(
                    new Run(
                        new Text($"{user.Email} | {user.Phone}"))),
                new Paragraph(new Run(new Text(" "))) // Spacer
            ));

        var body = mainPart.Document.Body!;
        foreach (var section in sections.Where(s => s.IsVisible).OrderBy(s => s.DisplayOrder))
        {
            body.AppendChild(new Paragraph(
                new Run(new RunProperties(new Bold(), new FontSize { Val = "24" }), new Text(section.Title))));
            body.AppendChild(new Paragraph(
                new Run(new Text(section.Content))));
            body.AppendChild(new Paragraph()); // Spacer
        }
        
        doc.Save();
        return ms.ToArray();
    }

    // ─── Local/Azure Storage upload ────────────────────────────────

    private async Task<string> UploadToBlobAsync(string jobId, byte[] data, string contentType)
    {
        var connString = config["AzureBlob:ConnectionString"];
        var containerName = config["AzureBlob:ContainerName"] ?? "resume-exports";

        // FALLBACK: Local Storage if Azure is not configured
        if (string.IsNullOrEmpty(connString))
        {
            var extension = contentType switch
            {
                "application/pdf" => ".pdf",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                "application/json" => ".json",
                _ => ".bin"
            };

            var directory = Path.Combine(Directory.GetCurrentDirectory(), "exports");
            Directory.CreateDirectory(directory);
            var fileName = $"{jobId}{extension}";
            var path = Path.Combine(directory, fileName);
            
            await File.WriteAllBytesAsync(path, data);
            
            logger.LogInformation("Saved export locally to {Path}", path);
            return $"local://{fileName}";
        }

        var blobServiceClient = new BlobServiceClient(connString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(jobId);
        await blobClient.UploadAsync(new BinaryData(data),
            new Azure.Storage.Blobs.Models.BlobUploadOptions
            {
                HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders
                    { ContentType = contentType }
            });
        return blobClient.Uri.ToString();
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private async Task<ExportJob> CreateJobAsync(int userId, ExportRequest request, ExportFormat format)
    {
        var job = new ExportJob
        {
            ResumeId = request.ResumeId,
            UserId = userId,
            Format = format,
            TemplateId = 0, // caller should pass template ID if needed
            Customizations = request.Customizations
        };
        return await exportRepo.AddAsync(job);
    }

    private static ExportJobDto MapToDto(ExportJob j) =>
        new(j.JobId, j.ResumeId, j.UserId, j.Format, j.Status,
            j.FileUrl, j.FileSizeKb, j.RequestedAt, j.CompletedAt, j.ExpiresAt);
}
