namespace Learnix.Application.Messaging.Queries.GetOrStartConversation;

public sealed record ConversationDto(
    Guid Id,
    Guid CourseId,
    string CourseName,
    Guid OtherUserId,
    string OtherUserName,
    string? OtherUserAvatarPath,
    int UnreadCount);
