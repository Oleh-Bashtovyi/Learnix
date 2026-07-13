using Learnix.Domain.Enums;

namespace Learnix.Application.TestAttempts.Queries.GetTestAttemptReview;

/// <summary>
/// A student's own submitted attempt, disclosed as far as the test's <see cref="TestReviewMode"/> allows.
/// The mode travels with the payload so the client can say "your instructor does not show this" rather
/// than render a blank where an answer should be.
/// </summary>
/// <param name="Questions">Empty when the mode is <see cref="TestReviewMode.ScoreOnly"/>.</param>
public sealed record TestAttemptReviewResponse(
    Guid AttemptId,
    int AttemptNumber,
    int Score,
    int MaxScore,
    bool Passed,
    DateTime StartedAt,
    DateTime SubmittedAt,
    TestReviewMode ReviewMode,
    IReadOnlyList<ReviewedQuestionDto> Questions);

/// <param name="Answered">False when the student skipped the question entirely.</param>
/// <param name="IsCorrect">Null when the mode withholds correctness.</param>
/// <param name="Options">Null for TextInput questions.</param>
/// <param name="CorrectTextAnswer">Null unless the mode discloses correct answers.</param>
public sealed record ReviewedQuestionDto(
    int Order,
    string Text,
    QuestionType Type,
    bool Answered,
    bool? IsCorrect,
    IReadOnlyList<ReviewedOptionDto>? Options,
    IReadOnlyList<int>? StudentSelectedOptionOrders,
    string? StudentTextAnswer,
    string? CorrectTextAnswer);

/// <param name="IsCorrect">Null unless the mode discloses correct answers.</param>
public sealed record ReviewedOptionDto(int Order, string Text, bool? IsCorrect);
