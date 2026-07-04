namespace Learnix.Infrastructure.Outbox.Payloads.Achievements;

public sealed record EvaluateTestSubmittedPayload(
    Guid UserId,
    int QuestionsCount,
    int DurationSeconds,
    bool Passed);
