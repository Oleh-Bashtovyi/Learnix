using Learnix.API.Hubs;
using Learnix.Application.Common.Abstractions.Hubs;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Learnix.API.Services.Notifications;

internal sealed class SignalRNotificationSender(
    INotificationRepository notificationRepository,
    IHubContext<NotificationsHub, INotificationsHubClient> hubContext)
    : INotificationSender
{
    public async Task SendAsync(Guid userId, NotificationType type, string title, string body, CancellationToken ct = default)
    {
        var notification = Notification.Create(userId, type, title, body);
        await notificationRepository.AddAsync(notification, ct);
        await notificationRepository.TrimToMaxAsync(userId, NotificationConstants.MaxPerUser, ct);

        await hubContext.Clients
            .Group(NotificationsHub.UserGroup(userId.ToString()))
            .NotificationReceived(new NotificationReceivedPayload(
                notification.Id,
                type.ToString(),
                title,
                body,
                notification.CreatedAt));
    }
}
