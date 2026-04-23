using ResumeAI.Shared.Enums;
using System;

namespace ResumeAI.Resume.API.Entities;

public class ResumeSection
{
    public int SectionId { get; set; }
    public int ResumeId { get; set; }
    public SectionType SectionType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool AiGenerated { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
