using ResumeAI.Shared.Enums;

namespace ResumeAI.Notification.API.Entities;

public class NotificationRecord
{
    public int NotificationId { get; set; }
    public int RecipientId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; } = NotificationChannel.APP;
    public string? RelatedId { get; set; }
    public string? RelatedType { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
