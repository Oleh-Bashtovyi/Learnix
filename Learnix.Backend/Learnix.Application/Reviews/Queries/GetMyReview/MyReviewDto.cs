namespace Learnix.Application.Reviews.Queries.GetMyReview;

public sealed record MyReviewDto(
    Guid Id,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    DateTime UpdatedAt);
