using ResumeAI.Shared.Enums;

using ResumeAI.Export.API.Interfaces;

namespace ResumeAI.Export.API.Services;

public sealed class HttpNotificationPublisher(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<HttpNotificationPublisher> logger) : INotificationPublisher
{
    public async Task PublishAsync(int recipientId, string title, string message,
        NotificationType type, string? relatedId = null, string? relatedType = null, string? recipientEmail = null)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Notification");
            var key    = config["Notifications:InternalKey"] ?? string.Empty;

            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/internal")
            {
                Content = JsonContent.Create(new
                {
                    recipientId, title, message,
                    type        = type.ToString(),
                    relatedId,
                    relatedType,
                    recipientEmail
                })
            };
            req.Headers.Add("X-Internal-Key", key);

            var res = await client.SendAsync(req);
            if (!res.IsSuccessStatusCode)
                logger.LogWarning("Notification publish failed: {Status}", res.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish notification to Notification API.");
        }
    }
}