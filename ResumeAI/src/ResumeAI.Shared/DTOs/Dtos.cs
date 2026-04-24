using ResumeAI.Shared.Enums;

namespace ResumeAI.Shared.DTOs;

// ─── Auth DTOs ──────────────────────────────────────────────────
public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string? Phone = null);

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public record UserDto(
    int UserId,
    string FullName,
    string Email,
    string? Phone,
    Role Role,
    AuthProvider Provider,
    bool IsActive,
    SubscriptionPlan SubscriptionPlan,
    DateTime CreatedAt);

public record UpdateProfileRequest(
    string FullName,
    string Email,
    string? Phone);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public record UpdateSubscriptionRequest(SubscriptionPlan Plan);

// ─── Resume DTOs ────────────────────────────────────────────────
public record CreateResumeRequest(
    string Title,
    string TargetJobTitle,
    int TemplateId = 1,
    string Language = "en");

public record UpdateResumeRequest(
    string Title,
    string TargetJobTitle,
    int TemplateId,
    string Language,
    ResumeStatus Status);

public record ResumeDto(
    int ResumeId,
    int UserId,
    string Title,
    string TargetJobTitle,
    int TemplateId,
    int AtsScore,
    ResumeStatus Status,
    string Language,
    bool IsPublic,
    int ViewCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IList<SectionDto>? Sections = null);

// ─── Section DTOs ───────────────────────────────────────────────
public record AddSectionRequest(
    int ResumeId,
    SectionType SectionType,
    string Title,
    string Content,
    int DisplayOrder,
    bool IsVisible = true);

public record UpdateSectionRequest(
    string Title,
    string Content,
    int DisplayOrder,
    bool IsVisible,
    bool? AiGenerated = null);

public record SectionDto(
    int SectionId,
    int ResumeId,
    SectionType SectionType,
    string Title,
    string Content,
    int DisplayOrder,
    bool IsVisible,
    bool AiGenerated,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ReorderSectionsRequest(IList<int> OrderedSectionIds);

public record BulkUpdateSectionsRequest(IList<UpdateSectionItem> Sections);

public record UpdateSectionItem(
    int SectionId,
    string Title,
    string Content,
    int DisplayOrder,
    bool IsVisible,
    bool? AiGenerated = null);

// ─── Template DTOs ──────────────────────────────────────────────
public record CreateTemplateRequest(
    string Name,
    string Description,
    string ThumbnailUrl,
    string HtmlLayout,
    string CssStyles,
    TemplateCategory Category,
    bool IsPremium);

public record UpdateTemplateRequest(
    string Name,
    string Description,
    string ThumbnailUrl,
    string HtmlLayout,
    string CssStyles,
    TemplateCategory Category,
    bool IsPremium);

public record TemplateDto(
    int TemplateId,
    string Name,
    string Description,
    string ThumbnailUrl,
    string HtmlLayout,
    string CssStyles,
    TemplateCategory Category,
    bool IsPremium,
    bool IsActive,
    int UsageCount,
    DateTime CreatedAt);

public record TemplatePreviewDto(
    int TemplateId,
    string HtmlLayout,
    string CssStyles);

// ─── AI DTOs ────────────────────────────────────────────────────
public record GenerateSummaryRequest(
    int ResumeId,
    string JobTitle,
    int YearsOfExperience,
    string KeySkills);

public record GenerateBulletsRequest(
    int ResumeId,
    string JobTitle,
    string CompanyName,
    string Responsibilities);

public record GenerateCoverLetterRequest(
    int ResumeId,
    string JobDescription,
    string CompanyName);

public record ImproveSectionRequest(
    int ResumeId,
    int SectionId,
    string CurrentContent,
    string ImprovementHint = "");

public record CheckAtsRequest(
    int ResumeId,
    string JobDescription);

public record SuggestSkillsRequest(
    int ResumeId,
    string TargetJobTitle);

public record TailorResumeRequest(
    int ResumeId,
    string JobDescription);

public record TranslateResumeRequest(
    int ResumeId,
    string TargetLanguage);

public record AiRequestDto(
    string RequestId,
    int UserId,
    int ResumeId,
    AiRequestType RequestType,
    string InputPrompt,
    string AiResponse,
    AiModel Model,
    int TokensUsed,
    AiRequestStatus Status,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public record AiQuotaDto(
    int RemainingContentCalls,
    int RemainingAtsCalls,
    int MaxContentCalls,
    int MaxAtsCalls);

// ─── Export DTOs ────────────────────────────────────────────────
public record ExportRequest(
    int ResumeId,
    string? Customizations = null);

public record ExportJobDto(
    string JobId,
    int ResumeId,
    int UserId,
    ExportFormat Format,
    ExportStatus Status,
    string? FileUrl,
    long FileSizeKb,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    DateTime? ExpiresAt);

// ─── JobMatch DTOs ──────────────────────────────────────────────
public record AnalyzeJobFitRequest(
    int ResumeId,
    string JobTitle,
    string JobDescription,
    string? CompanyName = null,
    string? Location = null,
    JobMatchSource Source = JobMatchSource.MANUAL);

public record JobMatchDto(
    int MatchId,
    int ResumeId,
    int UserId,
    string JobTitle,
    string JobDescription,
    string? CompanyName,
    string? Location,
    int MatchScore,
    string MissingSkills,
    string Recommendations,
    JobMatchSource Source,
    DateTime MatchedAt,
    bool IsBookmarked);

// ─── Notification DTOs ──────────────────────────────────────────
public record NotificationDto(
    int NotificationId,
    int RecipientId,
    NotificationType Type,
    string Title,
    string Message,
    NotificationChannel Channel,
    string? RelatedId,
    string? RelatedType,
    bool IsRead,
    DateTime SentAt);

public record SendBulkNotificationRequest(
    string Title,
    string Message,
    NotificationType Type,
    SubscriptionPlan? TargetPlan = null);

// ─── Shared response wrapper ─────────────────────────────────────
public record ApiResponse<T>(bool Success, T? Data, string? Error = null)
{
    public static ApiResponse<T> Ok(T data) => new(true, data);
    public static ApiResponse<T> Fail(string error) => new(false, default, error);
}

public record PaginationRequest(int Page = 1, int PageSize = 10);

public record PagedResponse<T>(IList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
