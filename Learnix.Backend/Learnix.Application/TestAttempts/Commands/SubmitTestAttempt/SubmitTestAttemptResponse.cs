namespace Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;

public sealed record SubmitTestAttemptResponse(
    Guid AttemptId,
    int AttemptNumber,
    int Score,
    int MaxScore,
    bool Passed,
    DateTime SubmittedAt,
    IReadOnlyList<QuestionResultDto> QuestionResults);

public sealed record QuestionResultDto(int QuestionOrder, bool IsCorrect);
