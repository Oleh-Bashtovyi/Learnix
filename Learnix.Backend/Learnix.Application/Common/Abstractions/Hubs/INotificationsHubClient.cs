namespace Learnix.Application.Common.Abstractions.Hubs;

public interface INotificationsHubClient
{
    Task ReceiveMessage(NewMessageNotification notification);
    Task UnreadCountChanged(UnreadCountNotification notification);
    Task AchievementUnlocked(AchievementUnlockedNotification notification);
    Task CertificateReady(CertificateReadyNotification notification);
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
