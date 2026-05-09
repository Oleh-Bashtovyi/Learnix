using Learnix.Application.Messaging.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Learnix.Infrastructure.Hubs;

[Authorize]
public sealed class ChatHub : Hub<IChatHubClient>
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

    public static string UserGroup(string userId) => $"chat-user-{userId}";
}
