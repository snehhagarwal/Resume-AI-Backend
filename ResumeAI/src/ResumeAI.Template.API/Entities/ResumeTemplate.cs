using ResumeAI.Shared.Enums;

namespace ResumeAI.Template.API.Entities;

public class ResumeTemplate
{
    public int TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    /// <summary>Full HTML layout stored as TEXT column.</summary>
    public string HtmlLayout { get; set; } = string.Empty;
    /// <summary>Full CSS styles stored as TEXT column.</summary>
    public string CssStyles { get; set; } = string.Empty;
    public TemplateCategory Category { get; set; }
    public bool IsPremium { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
