namespace Learnix.Infrastructure.Outbox.Payloads.Achievements;

public sealed record EvaluateEnrollmentCompletedPayload(Guid UserId, Guid CourseId);
