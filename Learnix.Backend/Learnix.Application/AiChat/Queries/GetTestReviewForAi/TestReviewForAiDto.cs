using System.Text.Json.Serialization;
using Learnix.Application.AiChat.Tools;
using Learnix.Domain.Enums;

namespace Learnix.Application.AiChat.Queries.GetTestReviewForAi;

/// <summary>
/// A submitted attempt, as the tutor is allowed to see it. Unlike <c>LessonForAiDto</c> this one does carry
/// the questions and the correct answers — the platform already revealed them to the student in
/// <c>SubmitTestAttemptResponse</c> the moment they submitted. See ADR-BACK-CHAT-012.
/// </summary>
public sealed record TestReviewForAiDto(
    [property: JsonPropertyName(ChatToolJson.LessonIdProperty)] Guid LessonId,
    string Title,
    int AttemptNumber,
    int Score,
    int MaxScore,
    bool Passed,
    DateTime SubmittedAt,
    IReadOnlyList<QuestionReviewDto> Questions);

/// <param name="Answered">False when the student skipped the question entirely.</param>
public sealed record QuestionReviewDto(
    int Order,
    string Text,
    QuestionType Type,
    bool Answered,
    bool IsCorrect,
    IReadOnlyList<OptionReviewDto>? Options = null,
    IReadOnlyList<int>? StudentSelectedOptionOrders = null,
    string? CorrectTextAnswer = null,
    string? StudentTextAnswer = null);

public sealed record OptionReviewDto(int Order, string Text, bool IsCorrect);
