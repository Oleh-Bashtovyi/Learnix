namespace Learnix.Application.Courses.Queries.GetFeaturedCourses;

public sealed record FeaturedCourseDto(
    Guid Id,
    string Title,
    string Description,
    string? CoverImageUrl,
    decimal Price,
    bool IsFree,
    decimal Rating,
    int ReviewsCount,
    double DurationHours,
    string CategoryName,
    FeaturedCourseInstructorDto Instructor,
    string? Badge);

public sealed record FeaturedCourseInstructorDto(Guid Id, string FullName);
