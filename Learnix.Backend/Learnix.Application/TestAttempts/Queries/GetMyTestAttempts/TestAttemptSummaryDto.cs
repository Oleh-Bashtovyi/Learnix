namespace Learnix.Application.TestAttempts.Queries.GetMyTestAttempts;

public sealed record TestAttemptSummaryDto(
    Guid AttemptId,
    int AttemptNumber,
    int Score,
    int MaxScore,
    bool Passed,
    DateTime StartedAt,
    DateTime SubmittedAt);
