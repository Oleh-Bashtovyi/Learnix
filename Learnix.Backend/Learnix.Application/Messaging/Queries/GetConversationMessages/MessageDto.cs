namespace Learnix.Application.Messaging.Queries.GetConversationMessages;

public sealed record MessageDto(
    Guid Id,
    Guid SenderId,
    string SenderName,
    string? SenderAvatarPath,
    string Content,
    DateTime SentAt,
    bool IsFromCurrentUser);
