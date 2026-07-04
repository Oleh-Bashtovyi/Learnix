namespace Learnix.Application.Courses.Queries.GetInstructorCourses;

public sealed record ManageCourseCardDto(
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
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsDeleted = false);
