using ResumeAI.Shared.Enums;

namespace ResumeAI.JobMatch.API.Interfaces
{
    public interface INotificationPublisher
    {
        Task PublishAsync(int recipientId, string title, string message,
            NotificationType type, string? relatedId = null, string? relatedType = null, string? recipientEmail = null);
    }
}
