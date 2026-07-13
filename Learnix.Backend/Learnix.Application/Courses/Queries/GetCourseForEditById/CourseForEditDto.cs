using Learnix.Domain.Enums;
namespace Learnix.Application.Courses.Queries.GetCourseForEditById;

public sealed record CourseForEditDto(
    Guid Id,
    Guid InstructorId,
    Guid CategoryId,
    string Title,
    string Description,
    string? CoverImageUrl,
    decimal Price,
    bool IsFree,
    string Status,
    int EnrollmentsCount,
    IReadOnlyList<string> Tags,
    IReadOnlyList<CourseForEditSectionDto> Sections,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CourseForEditSectionDto(
    Guid Id,
    string Title,
    int Order,
    IReadOnlyList<CourseForEditLessonDto> Lessons);

public sealed record CourseForEditLessonDto(
    Guid Id,
    string Title,
    int Order,
    string LessonType,
    bool IsHidden,
    string? VideoUrl,
    string? Description,
    int? DurationSeconds,
    string? Content,
    int? AttemptLimit,
    int? CooldownMinutes,
    int? PassingThreshold,
    TestReviewMode? ReviewMode,
    IReadOnlyList<CourseForEditQuestionDto> Questions);

public sealed record CourseForEditQuestionDto(
    Guid Id,
    string Text,
    string Type,
    int Order,
    IReadOnlyList<CourseForEditQuestionOptionDto> Options,
    string? CorrectAnswer,
    bool IgnoreCase,
    bool AllowFuzzy);

public sealed record CourseForEditQuestionOptionDto(
    Guid Id,
    string Text,
    bool IsCorrect,
    int Order);
