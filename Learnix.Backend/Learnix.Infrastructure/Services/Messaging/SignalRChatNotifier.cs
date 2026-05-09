using Learnix.Application.Messaging.Abstractions;
using Learnix.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Learnix.Infrastructure.Services.Messaging;

internal sealed class SignalRChatNotifier(
    IHubContext<ChatHub, IChatHubClient> hubContext)
    : IChatNotifier
{
    public Task NotifyNewMessageAsync(Guid recipientId, NewMessageNotification notification, CancellationToken ct)
        => hubContext.Clients
            .Group(ChatHub.UserGroup(recipientId.ToString()))
            .ReceiveMessage(notification);

    public Task NotifyUnreadCountChangedAsync(Guid userId, int totalUnread, CancellationToken ct)
        => hubContext.Clients
            .Group(ChatHub.UserGroup(userId.ToString()))
            .UnreadCountChanged(new UnreadCountNotification(totalUnread));
}
