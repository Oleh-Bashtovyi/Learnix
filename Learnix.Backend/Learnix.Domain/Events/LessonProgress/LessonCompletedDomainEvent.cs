using Learnix.Domain.Common;

namespace Learnix.Domain.Events.LessonProgress;

public sealed record LessonCompletedDomainEvent(
    Guid StudentId,
    Guid CourseId,
    Guid LessonId
) : DomainEvent;
