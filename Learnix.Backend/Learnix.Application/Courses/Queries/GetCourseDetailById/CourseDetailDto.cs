namespace Learnix.Application.Courses.Queries.GetCourseById;

public sealed record CourseDetailDto(
    Guid Id,
    Guid InstructorId,
    Guid CategoryId,
    string Title,
    string Description,
    string? CoverImageUrl,
    decimal Price,
    bool IsFree,
    int EnrollmentsCount,
    decimal AverageRating,
    int ReviewsCount,
    IReadOnlyList<string> Tags,
    IReadOnlyList<SectionDto> Sections,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string InstructorFullName);

public sealed record SectionDto(
    Guid Id,
    string Title,
    int Order,
    IReadOnlyList<LessonSummaryDto> Lessons);

public sealed record LessonSummaryDto(
    Guid Id,
    string Title,
    int Order,
    string LessonType);
