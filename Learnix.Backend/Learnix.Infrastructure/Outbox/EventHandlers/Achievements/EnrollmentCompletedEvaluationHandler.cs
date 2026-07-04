using Learnix.Domain.Events.Enrollments;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

internal sealed class EnrollmentCompletedEvaluationHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<EnrollmentCompletedDomainEvent, EvaluateEnrollmentCompletedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.EvaluateEnrollmentCompleted;
    protected override EvaluateEnrollmentCompletedPayload BuildPayload(EnrollmentCompletedDomainEvent e)
        => new(e.StudentId, e.CourseId);
}
