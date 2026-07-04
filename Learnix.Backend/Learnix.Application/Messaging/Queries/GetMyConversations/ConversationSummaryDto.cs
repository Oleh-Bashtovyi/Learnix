namespace Learnix.Application.Messaging.Queries.GetMyConversations;

public sealed record ConversationSummaryDto(
    Guid Id,
    Guid CourseId,
    string CourseName,
    Guid OtherUserId,
    string OtherUserName,
    string? OtherUserAvatarPath,
    string? LastMessagePreview,
    DateTime? LastMessageAt,
    int UnreadCount);
