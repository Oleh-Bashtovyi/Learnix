using Learnix.Domain.Events.LessonProgress;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

internal sealed class LessonCompletedEvaluationHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<LessonCompletedDomainEvent, EvaluateLessonCompletedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.EvaluateLessonCompleted;
    protected override EvaluateLessonCompletedPayload BuildPayload(LessonCompletedDomainEvent e)
        => new(e.StudentId);
}
