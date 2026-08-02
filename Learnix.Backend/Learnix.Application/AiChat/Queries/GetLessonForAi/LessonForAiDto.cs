using System.Text.Json.Serialization;
using Learnix.Application.AiChat.Tools;
using Learnix.Domain.Enums;

namespace Learnix.Application.AiChat.Queries.GetLessonForAi;

/// <summary>
/// The lesson as the model is allowed to see it. Deliberately not <c>LessonContentDto</c>: that one carries a
/// signed blob URL for the video, which must never leave the server for a third-party model provider.
/// Test questions and their answers are absent by design — see ADR-BACK-CHAT-012.
/// </summary>
/// <param name="ContentAvailable">False for video lessons: there is no transcript and the model cannot watch it.</param>
/// <param name="Content">Body of a written lesson, possibly truncated. Null for video and test lessons.</param>
/// <param name="ContentUnavailableReason">Why the model cannot see the substance of this lesson, if it cannot.</param>
public sealed record LessonForAiDto(
    [property: JsonPropertyName(ChatToolJson.LessonIdProperty)] Guid LessonId,
    string Title,
    LessonType LessonType,
    bool ContentAvailable,
    string? Description = null,
    string? Content = null,
    bool ContentTruncated = false,
    int? DurationSeconds = null,
    TestInfoDto? Test = null,
    string? ContentUnavailableReason = null);

/// <param name="SubmittedAttempts">How many attempts the student has already submitted.</param>
/// <param name="ReviewAvailable">
/// Whether `get_my_test_review` will succeed: at least one submitted attempt and none currently open.
/// </param>
public sealed record TestInfoDto(
    int QuestionCount,
    int PassingThreshold,
    int? AttemptLimit,
    int? CooldownMinutes,
    int SubmittedAttempts,
    bool ReviewAvailable);
