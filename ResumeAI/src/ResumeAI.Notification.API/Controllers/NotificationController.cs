using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Notification.API.Services;
using ResumeAI.Notification.API.Interfaces;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;

namespace ResumeAI.Notification.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController(
    INotificationService notifService,
    IConfiguration config) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());

    // ── User-facing endpoints ─────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetByRecipient()
    {
        var notifs = await notifService.GetByRecipientAsync(CurrentUserId);
        return Ok(ApiResponse<IList<NotificationDto>>.Ok(notifs));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await notifService.GetUnreadCountAsync(CurrentUserId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPut("{notificationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        await notifService.MarkAsReadAsync(notificationId);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await notifService.MarkAllReadAsync(CurrentUserId);
        return NoContent();
    }

    [HttpDelete("{notificationId:int}")]
    public async Task<IActionResult> Delete(int notificationId)
    {
        await notifService.DeleteAsync(notificationId);
        return NoContent();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost("bulk")]
    public async Task<IActionResult> SendBulk(
        [FromBody] SendBulkNotificationRequest request,
        [FromQuery] List<int>? recipientIds = null)
    {
        var ids = recipientIds ?? new List<int>();
        await notifService.SendBulkAsync(request, ids);
        return NoContent();
    }

    // ── Internal service-to-service endpoint ──────────────────────
    // Called by AI, Export and JobMatch services using a shared secret.
    // No user JWT required — the recipientId is supplied in the body.

    [AllowAnonymous]
    [HttpPost("internal")]
    public async Task<IActionResult> SendInternal(
        [FromBody] InternalNotificationRequest request)
    {
        var expectedKey = config["Notifications:InternalKey"];
        if (string.IsNullOrEmpty(expectedKey))
            return StatusCode(503, "Internal notifications not configured.");

        var providedKey = Request.Headers["X-Internal-Key"].ToString();
        if (providedKey != expectedKey)
            return Unauthorized("Invalid internal key.");

        await notifService.SendAsync(
            request.RecipientId,
            request.Title,
            request.Message,
            request.Type,
            request.Channel,
            request.RelatedId,
            request.RelatedType,
            request.RecipientEmail);

        return NoContent();
    }
}

/// <summary>Payload for the internal service-to-service notification endpoint.</summary>
public record InternalNotificationRequest(
    int RecipientId,
    string Title,
    string Message,
    NotificationType Type,
    NotificationChannel Channel = NotificationChannel.APP,
    string? RecipientEmail = null,
    string? RelatedId = null,
    string? RelatedType = null);