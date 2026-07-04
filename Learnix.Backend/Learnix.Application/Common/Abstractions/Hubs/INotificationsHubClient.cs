namespace Learnix.Application.Common.Abstractions.Hubs;

public interface INotificationsHubClient
{
    Task ReceiveMessage(NewMessageNotification notification);
    Task UnreadCountChanged(UnreadCountNotification notification);
    Task AchievementUnlocked(AchievementUnlockedNotification notification);
    Task CertificateReady(CertificateReadyNotification notification);
    Task NotificationReceived(NotificationReceivedPayload notification);
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

public sealed record AchievementUnlockedNotification(
    Guid AchievementId,
    string Code,
    DateTime UnlockedAt);

public sealed record CertificateReadyNotification(
    Guid CertificateId,
    string CourseTitle);

public sealed record NotificationReceivedPayload(
    Guid NotificationId,
    string Type,
    string Title,
    string Body,
    DateTime CreatedAt);
