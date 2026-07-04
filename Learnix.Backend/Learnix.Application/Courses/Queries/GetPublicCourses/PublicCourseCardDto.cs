namespace Learnix.Application.Courses.Queries.GetPublicCourses;

public sealed record PublicCourseCardDto(
    Guid Id,
    Guid InstructorId,
    Guid CategoryId,
    string Title,
    string Description,
    string? CoverImageUrl,
    decimal Price,
    bool IsFree,
    int EnrollmentsCount,
    IReadOnlyList<string> Tags,
    decimal AverageRating,
    int ReviewsCount,
    string CategoryName,
    string InstructorFullName);
