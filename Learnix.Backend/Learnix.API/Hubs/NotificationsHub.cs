using System.Security.Claims;
using Learnix.Application.Common.Abstractions.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Learnix.API.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub<INotificationsHubClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue("sub");
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue("sub");
        if (userId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId));

        await base.OnDisconnectedAsync(exception);
    }

    public static string UserGroup(string userId) => $"user-{userId}";
}
