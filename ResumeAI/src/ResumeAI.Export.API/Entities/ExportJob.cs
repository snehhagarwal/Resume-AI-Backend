using ResumeAI.Shared.Enums;

namespace ResumeAI.Export.API.Entities;

public class ExportJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public int ResumeId { get; set; }
    public int UserId { get; set; }
    public ExportFormat Format { get; set; }
    public ExportStatus Status { get; set; } = ExportStatus.QUEUED;
    public string? FileUrl { get; set; }
    public long FileSizeKb { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int TemplateId { get; set; }
    /// <summary>JSON string for custom font/color options.</summary>
    public string? Customizations { get; set; }
}
