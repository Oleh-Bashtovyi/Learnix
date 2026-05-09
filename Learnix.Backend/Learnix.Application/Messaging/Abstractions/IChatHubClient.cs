namespace Learnix.Application.Messaging.Abstractions;

public interface IChatHubClient
{
    Task ReceiveMessage(NewMessageNotification notification);
    Task UnreadCountChanged(UnreadCountNotification notification);
}

public sealed record NewMessageNotification(
    Guid ConversationId,
    Guid MessageId,
    Guid SenderId,
    string SenderName,
    string? SenderAvatarPath,
    string Content,
    DateTime SentAt);

public sealed record UnreadCountNotification(int TotalUnread);
