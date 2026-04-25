using ResumeAI.Shared.Enums;

namespace ResumeAI.JobMatch.API.Services;

/// <summary>
/// Fires a fire-and-forget notification to the Notification API via the
/// internal shared-secret endpoint. Failures are swallowed + logged so
/// a notification hiccup never kills a job-match analysis.
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync(int recipientId, string title, string message,
        NotificationType type, string? relatedId = null, string? relatedType = null);
}

public sealed class HttpNotificationPublisher(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<HttpNotificationPublisher> logger) : INotificationPublisher
{
    public async Task PublishAsync(int recipientId, string title, string message,
        NotificationType type, string? relatedId = null, string? relatedType = null)
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
                    relatedType
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