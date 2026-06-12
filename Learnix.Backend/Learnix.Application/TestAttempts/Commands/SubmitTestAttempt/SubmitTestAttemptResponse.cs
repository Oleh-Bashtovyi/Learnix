namespace Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;

public sealed record SubmitTestAttemptResponse(
    Guid AttemptId,
    int AttemptNumber,
    int Score,
    int MaxScore,
    bool Passed,
    DateTime SubmittedAt,
    IReadOnlyList<QuestionResultDto> QuestionResults);

/// <param name="QuestionOrder">0-based order matching the question in the test.</param>
/// <param name="IsCorrect">Whether the student answered this question correctly.</param>
/// <param name="CorrectOptionOrders">
///     For SingleChoice/MultipleChoice: orders of correct option(s), so the UI can highlight them.
///     Null for TextInput questions.
/// </param>
public sealed record QuestionResultDto(
    int QuestionOrder,
    bool IsCorrect,
    IReadOnlyList<int>? CorrectOptionOrders,
    string? CorrectTextAnswer = null);
