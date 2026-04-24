namespace ResumeAI.Shared.Enums;

public enum Role { USER, ADMIN }

public enum SubscriptionPlan { FREE, PREMIUM }

public enum AuthProvider { LOCAL, GOOGLE, LINKEDIN }

public enum ResumeStatus { DRAFT, COMPLETE }

public enum SectionType
{
    SUMMARY,
    EXPERIENCE,
    EDUCATION,
    SKILLS,
    CERTIFICATIONS,
    PROJECTS,
    LANGUAGES,
    VOLUNTEER,
    CUSTOM
}

public enum TemplateCategory
{
    PROFESSIONAL,
    CREATIVE,
    MODERN,
    MINIMALIST,
    ATS_OPTIMISED
}

public enum ExportFormat { PDF, DOCX, JSON }

public enum ExportStatus { QUEUED, PROCESSING, COMPLETED, FAILED }

public enum AiRequestType
{
    SUMMARY,
    BULLETS,
    COVER_LETTER,
    IMPROVE,
    ATS,
    SKILLS,
    TAILOR,
    TRANSLATE,
    JOB_MATCH
}

public enum AiModel { GPT4O, CLAUDE }

public enum AiRequestStatus { QUEUED, COMPLETED, FAILED }

public enum NotificationType
{
    ATS_COMPLETE,
    EXPORT_READY,
    AI_DONE,
    JOB_MATCH,
    PLAN_CHANGE,
    QUOTA_WARNING
}

public enum NotificationChannel { APP, EMAIL }

public enum JobMatchSource { LINKEDIN , MANUAL }
