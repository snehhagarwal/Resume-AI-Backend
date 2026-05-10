using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Notification.API.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> SendAsync(int recipientId, string title, string message,
            NotificationType type, NotificationChannel channel = NotificationChannel.APP,
            string? relatedId = null, string? relatedType = null, string? recipientEmail = null);
        Task SendBulkAsync(SendBulkNotificationRequest request, IList<int> recipientIds);
        Task<IList<NotificationDto>> GetByRecipientAsync(int recipientId);
        Task<int> GetUnreadCountAsync(int recipientId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllReadAsync(int recipientId);
        Task DeleteAsync(int notificationId);
    }
}
