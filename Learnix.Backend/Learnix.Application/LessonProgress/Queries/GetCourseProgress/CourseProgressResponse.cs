namespace Learnix.Application.LessonProgress.Queries.GetCourseProgress;

public sealed record CourseProgressResponse(
    Guid CourseId,
    int TotalLessons,
    int CompletedLessons,
    IReadOnlyList<SectionProgressDto> Sections);

public sealed record SectionProgressDto(
    Guid SectionId,
    string Title,
    int DisplayOrder,
    IReadOnlyList<LessonProgressItemDto> Lessons);

public sealed record LessonProgressItemDto(
    Guid LessonId,
    string Title,
    string LessonType,
    int DisplayOrder,
    bool IsCompleted,
    DateTime? CompletedAt,
    DateTime? LastAccessedAt);
