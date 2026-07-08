using Learnix.API.Hubs;
using Learnix.Application.Common.Abstractions.Hubs;
using Learnix.Application.Messaging.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Learnix.API.Services.Notifications;

internal sealed class SignalRChatNotifier(
    IHubContext<NotificationsHub, INotificationsHubClient> hubContext)
    : IChatNotifier
{
    public Task NotifyNewMessageAsync(Guid recipientId, NewMessageNotification notification, CancellationToken ct)
        => hubContext.Clients
            .Group(NotificationsHub.UserGroup(recipientId.ToString()))
            .ReceiveMessage(notification);

    public Task NotifyUnreadCountChangedAsync(Guid userId, int totalUnread, CancellationToken ct)
        => hubContext.Clients
            .Group(NotificationsHub.UserGroup(userId.ToString()))
            .UnreadCountChanged(new UnreadCountNotification(totalUnread));
}
