namespace Learnix.Application.Messaging.Abstractions;

public interface IChatNotifier
{
    Task NotifyNewMessageAsync(Guid recipientId, NewMessageNotification notification, CancellationToken ct);
    Task NotifyUnreadCountChangedAsync(Guid userId, int totalUnread, CancellationToken ct);
}
