using Learnix.Domain.Enums;

namespace Learnix.Application.TestAttempts.Queries.GetTestLesson;

public sealed record GetTestLessonResponse(
    Guid LessonId,
    string Title,
    string? Description,
    int PassingThreshold,
    int? AttemptLimit,
    int? CooldownMinutes,
    StudentTestStatusDto StudentStatus,
    IReadOnlyList<QuestionDto> Questions);

public sealed record StudentTestStatusDto(
    int AttemptsUsed,
    bool CanAttempt,
    int? CooldownRemainingMinutes,
    LatestAttemptDto? LatestAttempt);

public sealed record LatestAttemptDto(
    Guid AttemptId,
    int AttemptNumber,
    int Score,
    int MaxScore,
    bool Passed,
    DateTime SubmittedAt);

public sealed record QuestionDto(
    string Text,
    QuestionType Type,
    int Order,
    IReadOnlyList<QuestionOptionDto>? Options);

public sealed record QuestionOptionDto(string Text, int Order);
