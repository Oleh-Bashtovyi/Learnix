using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Lessons;

public sealed record LessonVideoAttachedDomainEvent(
    Guid LessonId,
    string AttachedBlobPath
) : DomainEvent;
