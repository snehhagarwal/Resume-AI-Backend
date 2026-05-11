using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using ResumeAI.Notification.API.Data;
using ResumeAI.Notification.API.Entities;
using ResumeAI.Notification.API.Hubs;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

using ResumeAI.Notification.API.Interfaces;

namespace ResumeAI.Notification.API.Services;

public class NotificationService(
    NotificationDbContext db,
    IConfiguration config,
    IHubContext<NotificationHub> hubContext,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<NotificationDto> SendAsync(
        int recipientId, string title, string message,
        NotificationType type, NotificationChannel channel = NotificationChannel.APP,
        string? relatedId = null, string? relatedType = null, string? recipientEmail = null)
    {
        var notification = new NotificationRecord
        {
            RecipientId = recipientId,
            Type = type,
            Title = title,
            Message = message,
            Channel = channel,
            RelatedId = relatedId,
            RelatedType = relatedType
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Trigger email if channel is EMAIL OR if a recipientEmail is explicitly provided
        if (channel == NotificationChannel.EMAIL || !string.IsNullOrEmpty(recipientEmail))
        {
            if (!string.IsNullOrEmpty(recipientEmail))
            {
                // Run email sending in the background and log errors if they occur
                _ = Task.Run(async () => 
                {
                    try 
                    {
                        await SendEmailAsync(recipientId, recipientEmail, title, message);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Background email delivery failed for recipient {RecipientId}", recipientId);
                    }
                });
            }
            else if (channel == NotificationChannel.EMAIL)
            {
                logger.LogWarning("Email channel requested for recipient {RecipientId} but no email provided.", recipientId);
            }
        }

        // Push full notification DTO + updated unread count via SignalR
        var dto = MapToDto(notification);
        var unreadCount = await GetUnreadCountAsync(recipientId);
        var userIdStr = recipientId.ToString();
        await hubContext.Clients.User(userIdStr)
            .SendAsync(NotificationHub.ReceiveNotification, dto);
        await hubContext.Clients.User(userIdStr)
            .SendAsync(NotificationHub.UnreadCountUpdated, unreadCount);

        return dto;
    }

    public async Task SendBulkAsync(SendBulkNotificationRequest request, IList<int> recipientIds)
    {
        var notifications = recipientIds.Select(id => new NotificationRecord
        {
            RecipientId = id,
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            Channel = NotificationChannel.APP
        }).ToList();

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();

        // Push full DTO + unread count to each recipient via SignalR
        var savedDtos = notifications.Select(MapToDto).ToList();
        foreach (var (notif, id) in savedDtos.Zip(recipientIds))
        {
            var unread = await GetUnreadCountAsync(id);
            var userIdStr = id.ToString();
            await hubContext.Clients.User(userIdStr)
                .SendAsync(NotificationHub.ReceiveNotification, notif);
            await hubContext.Clients.User(userIdStr)
                .SendAsync(NotificationHub.UnreadCountUpdated, unread);
        }
    }

    public async Task<IList<NotificationDto>> GetByRecipientAsync(int recipientId)
        => await db.Notifications
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.SentAt)
            .Select(n => MapToDto(n))
            .ToListAsync();

    public Task<int> GetUnreadCountAsync(int recipientId)
        => db.Notifications.CountAsync(n => n.RecipientId == recipientId && !n.IsRead);

    public async Task MarkAsReadAsync(int notificationId)
        => await db.Notifications
            .Where(n => n.NotificationId == notificationId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

    public async Task MarkAllReadAsync(int recipientId)
        => await db.Notifications
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

    public async Task DeleteAsync(int notificationId)
        => await db.Notifications
            .Where(n => n.NotificationId == notificationId)
            .ExecuteDeleteAsync();

    // ─── Email via MailKit ────────────────────────────────────────

    private async Task SendEmailAsync(int recipientId, string recipientEmail, string subject, string body)
    {
        try
        {
            var smtpHost = config["Smtp:Host"];
            var smtpPort = int.Parse(config["Smtp:Port"] ?? "587");
            var smtpUser = config["Smtp:Username"];
            var smtpPass = config["Smtp:Password"];
            var senderName = config["Smtp:SenderName"] ?? "NextHire";
            var senderEmail = config["Smtp:SenderEmail"] ?? smtpUser;

            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                logger.LogWarning("SMTP credentials missing — email not sent for recipient {RecipientId}", recipientId);
                return;
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(recipientEmail));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            
            // Use SslOnConnect for port 465, StartTls for others (like 587)
            var options = smtpPort == 465 
                ? SecureSocketOptions.SslOnConnect 
                : SecureSocketOptions.StartTls;

            await smtp.ConnectAsync(smtpHost, smtpPort, options);
            await smtp.AuthenticateAsync(smtpUser, smtpPass);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
            
            logger.LogInformation("Email sent successfully to {RecipientEmail}", recipientEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to recipient {RecipientId} at {Email}", recipientId, recipientEmail);
        }
    }

    private static NotificationDto MapToDto(NotificationRecord n) =>
        new(n.NotificationId, n.RecipientId, n.Type, n.Title,
            n.Message, n.Channel, n.RelatedId, n.RelatedType, n.IsRead, n.SentAt);
}