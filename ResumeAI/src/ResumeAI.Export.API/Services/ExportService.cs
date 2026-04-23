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
using ResumeAI.Export.API.Models;

namespace ResumeAI.Export.API.Services;

public class ExportService(
    IExportRepository exportRepo,
    IConfiguration config,
    IPdfRenderer pdfRenderer,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ExportService> logger) : IExportService
{
    // ... (Public API methods like ExportToPdfAsync, ExportToDocxAsync, etc. remain exactly as they are)

    public async Task<ExportJobDto> ExportToPdfAsync(int userId, ExportRequest request)
    {
        var job = await CreateJobAsync(userId, request, ExportFormat.PDF);
        try
        {
            job.Status = ExportStatus.PROCESSING;
            await exportRepo.UpdateAsync(job);
            var exportData = await GatherExportDataAsync(request.ResumeId, job.Customizations);
            var pdfBytes = pdfRenderer.GeneratePdf(exportData);
            job.FileUrl = await UploadToBlobAsync(job.JobId, pdfBytes, "application/pdf");
            job.FileSizeKb = pdfBytes.Length / 1024;
            job.Status = ExportStatus.COMPLETED;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex) { logger.LogError(ex, "PDF Fail"); job.Status = ExportStatus.FAILED; }
        return MapToDto(await exportRepo.UpdateAsync(job));
    }

    public async Task<ExportJobDto> ExportToDocxAsync(int userId, ExportRequest request)
    {
        var job = await CreateJobAsync(userId, request, ExportFormat.DOCX);
        try
        {
            job.Status = ExportStatus.PROCESSING;
            await exportRepo.UpdateAsync(job);
            var exportData = await GatherExportDataAsync(request.ResumeId, job.Customizations);
            var docxBytes = GenerateDocx(exportData);
            job.FileUrl = await UploadToBlobAsync(job.JobId, docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            job.FileSizeKb = docxBytes.Length / 1024;
            job.Status = ExportStatus.COMPLETED;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex) { logger.LogError(ex, "DOCX Fail"); job.Status = ExportStatus.FAILED; }
        return MapToDto(await exportRepo.UpdateAsync(job));
    }

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
        var job = await exportRepo.FindByJobIdAsync(jobId) ?? throw new KeyNotFoundException();
        if (job.FileUrl.StartsWith("local://")) return await File.ReadAllBytesAsync(Path.Combine(Directory.GetCurrentDirectory(), "exports", job.FileUrl.Replace("local://", "")));
        return (await new BlobClient(new Uri(job.FileUrl)).DownloadContentAsync()).Value.Content.ToArray();
    }

    public Task DeleteExportAsync(string jobId) => exportRepo.DeleteByJobIdAsync(jobId);
    public Task CleanupExpiredExportsAsync() => exportRepo.DeleteExpiredJobsAsync(DateTime.UtcNow);
    public async Task<IDictionary<string, int>> GetExportStatsAsync() {
        var stats = new Dictionary<string, int>();
        foreach (ExportStatus s in Enum.GetValues(typeof(ExportStatus))) stats[s.ToString()] = (await exportRepo.FindByStatusAsync(s)).Count;
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
        var res = await c.GetAsync($"{config["Services:ResumeApi"] ?? "http://localhost:5002"}/api/resumes/{id}");
        return (await res.Content.ReadFromJsonAsync<ApiResponse<ResumeDto>>(GetJsonOptions()))?.Data ?? throw new Exception();
    }

    private async Task<IList<SectionDto>> FetchSectionsAsync(int id) {
        var c = httpClientFactory.CreateClient(); ForwardToken(c);
        var res = await c.GetAsync($"{config["Services:SectionApi"] ?? "http://localhost:5003"}/api/sections/by-resume/{id}");
        return (await res.Content.ReadFromJsonAsync<ApiResponse<IList<SectionDto>>>(GetJsonOptions()))?.Data ?? new List<SectionDto>();
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

    private async Task<string> UploadToBlobAsync(string id, byte[] data, string type) {
        var conn = config["AzureBlob:ConnectionString"];
        if (string.IsNullOrEmpty(conn)) {
            var ext = type switch { "application/pdf" => ".pdf", "application/json" => ".json", _ => ".docx" };
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "exports"); Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(Path.Combine(dir, id + ext), data); return $"local://{id + ext}";
        }
        var container = new BlobServiceClient(conn).GetBlobContainerClient(config["AzureBlob:ContainerName"] ?? "resume-exports");
        await container.CreateIfNotExistsAsync();
        var client = container.GetBlobClient(id);
        await client.UploadAsync(new BinaryData(data), new Azure.Storage.Blobs.Models.BlobUploadOptions { HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = type } });
        return client.Uri.ToString();
    }

    private async Task<ExportJob> CreateJobAsync(int uid, ExportRequest req, ExportFormat f) {
        var j = new ExportJob { ResumeId = req.ResumeId, UserId = uid, Format = f, Status = ExportStatus.QUEUED, Customizations = req.Customizations };
        return await exportRepo.AddAsync(j);
    }

    private static ExportJobDto MapToDto(ExportJob j) => j == null ? null : new(j.JobId, j.ResumeId, j.UserId, j.Format, j.Status, j.FileUrl, j.FileSizeKb, j.RequestedAt, j.CompletedAt, j.ExpiresAt);
    private static JsonSerializerOptions GetJsonOptions() => new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
}
