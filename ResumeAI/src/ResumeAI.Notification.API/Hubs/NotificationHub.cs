using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ResumeAI.Notification.API.Hubs;

/// <summary>
/// Real-time SignalR hub.
/// - Clients authenticate via JWT in the query-string (?access_token=…)
/// - On connect the user is added to a personal group keyed by their userId
/// - Server pushes two event types:
///   "ReceiveNotification"  — full NotificationDto payload
///   "UnreadCountUpdated"   — just the new int count (for the nav badge)
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    // Keep event names in one place — frontend & service must match these exactly.
    public const string ReceiveNotification  = "ReceiveNotification";
    public const string UnreadCountUpdated   = "UnreadCountUpdated";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }
}