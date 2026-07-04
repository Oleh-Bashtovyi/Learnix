namespace Learnix.Application.LessonProgress.Commands.MarkLessonComplete;

public sealed record MarkLessonCompleteResponse(
    Guid LessonProgressId,
    bool IsCompleted,
    DateTime? CompletedAt);
