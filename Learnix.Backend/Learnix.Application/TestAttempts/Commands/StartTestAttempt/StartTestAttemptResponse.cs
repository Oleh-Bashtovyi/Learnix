namespace Learnix.Application.TestAttempts.Commands.StartTestAttempt;

public sealed record StartTestAttemptResponse(
    Guid AttemptId,
    int AttemptNumber,
    DateTime StartedAt);
