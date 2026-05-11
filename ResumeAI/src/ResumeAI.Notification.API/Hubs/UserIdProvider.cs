using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace ResumeAI.Notification.API.Hubs;

/// <summary>
/// Tells SignalR which string to use as the "user identifier" when routing
/// hub messages via Clients.User(...). 
/// Without this, Context.UserIdentifier is always null because the default
/// IUserIdProvider looks for NameIdentifier which JWT middleware places at
/// ClaimTypes.NameIdentifier (the long URI form), not the short "nameidentifier".
/// </summary>
public sealed class JwtUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Check for both the standard ClaimTypes and the raw JWT "sub" claim
        return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? connection.User?.FindFirstValue("sub");
    }
}