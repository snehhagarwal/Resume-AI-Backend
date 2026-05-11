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
using Microsoft.Extensions.Caching.Distributed;

namespace ResumeAI.Export.API.Services;

public class ExportService(
    IExportRepository exportRepo,
    IConfiguration config,
    IPdfRenderer pdfRenderer,
    INotificationPublisher notificationPublisher,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IDistributedCache cache,
    ILogger<ExportService> logger) : IExportService
{
    // ... (Public API methods like ExportToPdfAsync, ExportToDocxAsync, etc. remain exactly as they are)

    public async Task<ExportJobDto> ExportToPdfAsync(int userId, ExportRequest request)
    {
        var job = await CreateJobAsync(userId, request, ExportFormat.PDF);
        string? userEmail = null;
        try
        {
            job.Status = ExportStatus.PROCESSING;
            await exportRepo.UpdateAsync(job);
            var exportData = await GatherExportDataAsync(request.ResumeId, job.Customizations);
            userEmail = exportData.User.Email;
            var pdfBytes = pdfRenderer.GeneratePdf(exportData);
            job.FileUrl = await UploadToBlobAsync(job.JobId, pdfBytes, "application/pdf");
            job.FileSizeKb = pdfBytes.Length / 1024;
            job.Status = ExportStatus.COMPLETED;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex) 
        { 
            logger.LogError(ex, "PDF Generation Failed for User {UserId}, Resume {ResumeId}. Error: {Msg}", userId, request.ResumeId, ex.Message); 
            job.Status = ExportStatus.FAILED; 
        }
        var result = MapToDto(await exportRepo.UpdateAsync(job));
        if (job.Status == ExportStatus.COMPLETED)
            await notificationPublisher.PublishAsync(
                userId, "PDF Export Ready 📄",
                $"Your resume PDF is ready to download.",
                NotificationType.EXPORT_READY,
                relatedId:   job.JobId,
                relatedType: "ExportJob",
                recipientEmail: userEmail);
        return result;
    }

    public async Task<ExportJobDto> ExportToDocxAsync(int userId, ExportRequest request)
    {
        var job = await CreateJobAsync(userId, request, ExportFormat.DOCX);
        string? userEmail = null;
        try
        {
            job.Status = ExportStatus.PROCESSING;
            await exportRepo.UpdateAsync(job);
            var exportData = await GatherExportDataAsync(request.ResumeId, job.Customizations);
            userEmail = exportData.User.Email;
            var docxBytes = GenerateDocx(exportData);
            job.FileUrl = await UploadToBlobAsync(job.JobId, docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            job.FileSizeKb = docxBytes.Length / 1024;
            job.Status = ExportStatus.COMPLETED;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex) { logger.LogError(ex, "DOCX Fail"); job.Status = ExportStatus.FAILED; }
        var result = MapToDto(await exportRepo.UpdateAsync(job));
        if (job.Status == ExportStatus.COMPLETED)
            await notificationPublisher.PublishAsync(
                userId, "DOCX Export Ready 📝",
                $"Your resume Word document is ready to download.",
                NotificationType.EXPORT_READY,
                relatedId:   job.JobId,
                relatedType: "ExportJob",
                recipientEmail: userEmail);
        return result;    }

    public async Task<ExportJobDto> ExportToJsonAsync(int userId, ExportRequest request)
    {
        var job = await CreateJobAsync(userId, request, ExportFormat.JSON);
        try
        {
            job.Status = ExportStatus.PROCESSING;
            await exportRepo.UpdateAsync(job);
            var exportData = await GatherExportDataAsync(request.ResumeId, job.Customizations);
            var payload = JsonSerializer.SerializeToUtf8Bytes(exportData);
            job.FileUrl = await UploadToBlobAsync(job.JobId, payload, "application/json");
            job.FileSizeKb = payload.Length / 1024;
            job.Status = ExportStatus.COMPLETED;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex) { logger.LogError(ex, "JSON Fail"); job.Status = ExportStatus.FAILED; }
        return MapToDto(await exportRepo.UpdateAsync(job));
    }

    public async Task<ExportJobDto?> GetJobStatusAsync(string jobId) => MapToDto(await exportRepo.FindByJobIdAsync(jobId));
    public async Task<IList<ExportJobDto>> GetExportsByUserAsync(int userId) => (await exportRepo.FindByUserIdAsync(userId)).Select(MapToDto).ToList();

    public async Task<byte[]> DownloadFileAsync(string jobId)
    {
        logger.LogInformation("Attempting to download file for JobId: {JobId}", jobId);
        var job = await exportRepo.FindByJobIdAsync(jobId) ?? throw new KeyNotFoundException();

        if (job.FileUrl.StartsWith("redis://"))
        {
            var key = job.FileUrl.Replace("redis://", "");
            logger.LogInformation("Fetching file from Redis with key: {Key}", key);
            var bytes = await cache.GetAsync(key);
            if (bytes == null)
            {
                logger.LogWarning("File not found in Redis for JobId: {JobId}", jobId);
                throw new FileNotFoundException("Resume file expired or not found in cache.");
            }
            logger.LogInformation("Successfully retrieved {Size} bytes from Redis", bytes.Length);
            return bytes;
        }

        if (job.FileUrl.StartsWith("local://"))
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "exports", job.FileUrl.Replace("local://", ""));
            logger.LogInformation("Fetching file from local storage: {Path}", path);
            return await File.ReadAllBytesAsync(path);
        }

        logger.LogInformation("Fetching file from Azure Blob: {Url}", job.FileUrl);
        return (await new BlobClient(new Uri(job.FileUrl)).DownloadContentAsync()).Value.Content.ToArray();
    }

    public Task DeleteExportAsync(string jobId) => exportRepo.DeleteByJobIdAsync(jobId);
    public Task CleanupExpiredExportsAsync() => exportRepo.DeleteExpiredJobsAsync(DateTime.UtcNow);
    public async Task<IDictionary<string, int>> GetExportStatsAsync(int userId) {
        var stats = new Dictionary<string, int>();
        foreach (ExportStatus s in Enum.GetValues(typeof(ExportStatus))) stats[s.ToString()] = await exportRepo.CountByStatusAndUserAsync(s, userId);
        return stats;
    }

    // ─── DYNAMIC DOCX GENERATOR (MATCHES PDF LOGIC) ──────────────

    private static byte[] GenerateDocx(ExportData data)
    {
        var template = data.Template;
        var html = template.HtmlLayout;
        var css = template.CssStyles;

        // 1. Merge Header Data
        html = html.Replace("{{FullName}}", data.User.FullName)
                   .Replace("{{Email}}", data.User.Email)
                   .Replace("{{Phone}}", data.User.Phone ?? "")
                   .Replace("{{TargetJobTitle}}", data.Resume.TargetJobTitle);

        // --- TARGETED SECTION ERASER ---
        foreach (ResumeAI.Shared.Enums.SectionType type in Enum.GetValues(typeof(ResumeAI.Shared.Enums.SectionType)))
        {
            var placeholder = $"{{{{{type}}}}}";
            var sectionData = data.Sections.FirstOrDefault(s => s.SectionType == type);
            
            // If the section should not be displayed
            if (sectionData == null || !sectionData.IsVisible || string.IsNullOrWhiteSpace(sectionData.Content))
            {
                // Find the section block that contains this placeholder and erase it
                // We search backwards for the nearest <section and forwards for the nearest </section>
                var regexPattern = $@"(?is)<section[^>]*>(?:(?!<section).)*?{System.Text.RegularExpressions.Regex.Escape(placeholder)}.*?</section>";
                html = System.Text.RegularExpressions.Regex.Replace(html, regexPattern, "");
                
                // Final safety: wipe the placeholder if it survived
                html = html.Replace(placeholder, "");
            }
            else
            {
                // Section has content! Replace normally
                html = html.Replace(placeholder, sectionData.Content);
            }
        }

        // 2. Combine HTML and CSS into a standalone document
        var fullHtml = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <style>
                    {css}
                    body {{ font-family: sans-serif; }}
                    /* Ensure background colors show up in Word */
                    * {{ -webkit-print-color-adjust: exact; }}
                </style>
            </head>
            <body>
                {html}
            </body>
            </html>";

        // 3. Use AltChunk to import the HTML/CSS into Word
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());

            var afip = mainPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.Html);
            using (var htmlStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fullHtml)))
            {
                afip.FeedData(htmlStream);
            }

            var altChunk = new AltChunk { Id = mainPart.GetIdOfPart(afip) };
            mainPart.Document.Body!.AppendChild(altChunk);
            mainPart.Document.Save();
        }
        
        return ms.ToArray();
    }

    // ─── Fetchers & Helpers (Stable) ─────────────────────────────

    private async Task<ExportData> GatherExportDataAsync(int rid, string? custJson)
    {
        var r = await FetchResumeAsync(rid);
        var s = await FetchSectionsAsync(rid);
        var u = await FetchUserAsync();
        var t = await FetchTemplateAsync(r.TemplateId);
        return new ExportData(r, u, s, new ExportCustomizations(), t);
    }

    private async Task<ResumeDto> FetchResumeAsync(int id) {
        var c = httpClientFactory.CreateClient(); ForwardToken(c);
        var url = $"{config["Services:ResumeApi"] ?? "http://localhost:5002"}/api/resumes/{id}";
        try {
            var res = await c.GetAsync(url);
            if (!res.IsSuccessStatusCode) throw new Exception($"Resume API returned {res.StatusCode}");
            return (await res.Content.ReadFromJsonAsync<ApiResponse<ResumeDto>>(GetJsonOptions()))?.Data ?? throw new Exception("Empty response");
        } catch (Exception ex) { logger.LogError(ex, "Failed to fetch resume from {Url}", url); throw; }
    }

    private async Task<IList<SectionDto>> FetchSectionsAsync(int id) {
        var c = httpClientFactory.CreateClient(); ForwardToken(c);
        var url = $"{config["Services:SectionApi"] ?? "http://localhost:5003"}/api/sections/by-resume/{id}";
        try {
            var res = await c.GetAsync(url);
            if (!res.IsSuccessStatusCode) throw new Exception($"Section API returned {res.StatusCode}");
            return (await res.Content.ReadFromJsonAsync<ApiResponse<IList<SectionDto>>>(GetJsonOptions()))?.Data ?? new List<SectionDto>();
        } catch (Exception ex) { logger.LogError(ex, "Failed to fetch sections from {Url}", url); throw; }
    }

    private async Task<UserDto> FetchUserAsync() {
        var c = httpClientFactory.CreateClient(); ForwardToken(c);
        var res = await c.GetAsync($"{config["Services:AuthApi"] ?? "http://localhost:5001"}/api/auth/profile");
        return (await res.Content.ReadFromJsonAsync<ApiResponse<UserDto>>(GetJsonOptions()))?.Data ?? throw new Exception();
    }

    private async Task<TemplateDto> FetchTemplateAsync(int id) {
        var c = httpClientFactory.CreateClient(); ForwardToken(c);
        var res = await c.GetAsync($"{config["Services:TemplateApi"] ?? "http://localhost:5004"}/api/templates/{id}");
        return (await res.Content.ReadFromJsonAsync<ApiResponse<TemplateDto>>(GetJsonOptions()))?.Data ?? new TemplateDto(id, "Fallback", "", "", "", "", TemplateCategory.PROFESSIONAL, false, true, 0, DateTime.UtcNow);
    }

    private void ForwardToken(HttpClient c) {
        var auth = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(auth)) c.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(auth);
    }

    private async Task<string> UploadToBlobAsync(string id, byte[] data, string type){
        var ext = type switch { "application/pdf" => ".pdf", "application/json" => ".json", _ => ".docx" };
        var redisKey = $"file_{id}{ext}";
        logger.LogInformation("Azure Blob not configured. Storing {Type} in Redis with key: {Key}", type, redisKey);
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
        await cache.SetAsync(redisKey, data, options);
        logger.LogInformation("Successfully stored in Redis. Size: {Size} KB", data.Length / 1024);
        return $"redis://{redisKey}";
    }

    private async Task<ExportJob> CreateJobAsync(int uid, ExportRequest req, ExportFormat f) {
        var j = new ExportJob { ResumeId = req.ResumeId, UserId = uid, Format = f, Status = ExportStatus.QUEUED, Customizations = req.Customizations };
        return await exportRepo.AddAsync(j);
    }

    private static ExportJobDto MapToDto(ExportJob j) => j == null ? null : new(j.JobId, j.ResumeId, j.UserId, j.Format, j.Status, j.FileUrl, j.FileSizeKb, j.RequestedAt, j.CompletedAt, j.ExpiresAt);
    private static JsonSerializerOptions GetJsonOptions() => new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
}
