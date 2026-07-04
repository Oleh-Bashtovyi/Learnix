using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Lessons;

public sealed record LessonVideoReleasedDomainEvent(
    Guid LessonId,
    string ReleasedBlobPath
) : DomainEvent;
