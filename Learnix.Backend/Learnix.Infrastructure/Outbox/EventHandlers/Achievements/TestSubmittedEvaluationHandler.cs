using Learnix.Domain.Events.TestAttempts;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

internal sealed class TestSubmittedEvaluationHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<TestSubmittedDomainEvent, EvaluateTestSubmittedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.EvaluateTestSubmitted;
    protected override EvaluateTestSubmittedPayload BuildPayload(TestSubmittedDomainEvent e)
        => new(e.StudentId, e.QuestionsCount, e.DurationSeconds, e.Passed);
}
