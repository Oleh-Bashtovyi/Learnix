using Learnix.Domain.Common;

namespace Learnix.Domain.Events.TestAttempts;

public sealed record TestSubmittedDomainEvent(
    Guid StudentId,
    Guid TestLessonId,
    Guid AttemptId,
    int QuestionsCount,
    int DurationSeconds,
    bool Passed
) : DomainEvent;
