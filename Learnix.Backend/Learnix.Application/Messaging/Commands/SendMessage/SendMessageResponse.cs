namespace Learnix.Application.Messaging.Commands.SendMessage;

public sealed record SendMessageResponse(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderId,
    string SenderName,
    string? SenderAvatarPath,
    string Content,
    DateTime SentAt);
