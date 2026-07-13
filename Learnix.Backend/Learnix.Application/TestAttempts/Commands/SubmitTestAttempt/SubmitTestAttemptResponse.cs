using Learnix.Domain.Enums;

namespace Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;

/// <param name="ReviewMode">
///     What the instructor lets the student see back. The client needs it to tell a field that is
///     withheld from one that is simply absent: without it, an empty <paramref name="QuestionResults"/>
///     reads as a bug rather than as the policy the instructor chose.
/// </param>
public sealed record SubmitTestAttemptResponse(
    Guid AttemptId,
    int AttemptNumber,
    int Score,
    int MaxScore,
    bool Passed,
    DateTime SubmittedAt,
    TestReviewMode ReviewMode,
    IReadOnlyList<QuestionResultDto> QuestionResults);

/// <param name="QuestionOrder">0-based order matching the question in the test.</param>
/// <param name="IsCorrect">
///     Whether the student answered this question correctly — null when the mode withholds
///     correctness (<see cref="TestReviewMode.AnswersOnly"/>).
/// </param>
/// <param name="CorrectOptionOrders">
///     For SingleChoice/MultipleChoice: orders of correct option(s), so the UI can highlight them.
///     Null for TextInput questions, and null below <see cref="TestReviewMode.FullReview"/>.
/// </param>
public sealed record QuestionResultDto(
    int QuestionOrder,
    bool? IsCorrect,
    IReadOnlyList<int>? CorrectOptionOrders,
    string? CorrectTextAnswer = null);
