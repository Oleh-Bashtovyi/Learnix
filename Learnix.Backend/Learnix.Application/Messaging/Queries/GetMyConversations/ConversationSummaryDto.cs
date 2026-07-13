namespace Learnix.Application.Messaging.Queries.GetMyConversations;

/// <param name="OtherUserIsInstructor">
///     Whether the other side of this thread is the course's instructor. A conversation always has one
///     of each, so this is known here without asking Identity — and the client needs it to decide whether
///     there is a public profile to link to. A student has no instructor profile, and offering one would
///     invite the instructor to go looking for their students' ratings and courses.
/// </param>
public sealed record ConversationSummaryDto(
    Guid Id,
    Guid CourseId,
    string CourseName,
    Guid OtherUserId,
    string OtherUserName,
    string? OtherUserAvatarPath,
    string? LastMessagePreview,
    DateTime? LastMessageAt,
    int UnreadCount,
    bool OtherUserIsInstructor);
