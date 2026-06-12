namespace Learnix.Application.Notifications.Queries.GetMyNotifications;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt);
