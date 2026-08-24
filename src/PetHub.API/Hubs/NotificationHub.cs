using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PetHub.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public const string ReceiveNotificationEvent = "ReceiveNotification";

    public static string GroupName(Guid userId) => $"user-{userId:D}";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(GetUserId()));
        await base.OnConnectedAsync();
    }

    private Guid GetUserId()
    {
        var value =
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (!Guid.TryParse(value, out var userId))
            throw new HubException("Unauthorized");

        return userId;
    }
}
