namespace Learnix.Application.Reviews.Queries.GetCourseReviews;

public sealed record CourseReviewDto(
    Guid Id,
    Guid StudentId,
    string StudentFirstName,
    string StudentLastName,
    string? StudentAvatarBlobPath,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    DateTime UpdatedAt);
