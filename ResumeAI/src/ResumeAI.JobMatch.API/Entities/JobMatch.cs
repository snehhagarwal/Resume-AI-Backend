using ResumeAI.Shared.Enums;

namespace ResumeAI.JobMatch.API.Entities;

public class JobMatch
{
    public int MatchId { get; set; }
    public int ResumeId { get; set; }
    public int UserId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public int MatchScore { get; set; }
    public string MissingSkills { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public JobMatchSource Source { get; set; } = JobMatchSource.MANUAL;
    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;
    public bool IsBookmarked { get; set; }
}
