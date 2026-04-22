using ResumeAI.Shared.Enums;

namespace ResumeAI.Resume.API.Entities;

public class ResumeRecord
{
    public int ResumeId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TargetJobTitle { get; set; } = string.Empty;
    public int TemplateId { get; set; }
    public int AtsScore { get; set; }
    public ResumeStatus Status { get; set; } = ResumeStatus.DRAFT;
    public string Language { get; set; } = "en";
    public bool IsPublic { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
