using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Reviews.Abstractions;

public interface ICourseReviewRepository : IRepositoryBase<CourseReview>
{
    Task<(int Count, decimal Average)> GetCourseRatingMetricsAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The student's own rating (1–5) for each of the given courses. Courses the student has not
    /// reviewed are absent from the result.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetMyRatingsAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// How many reviews across the given courses carry each rating (1–5), counted in the database.
    /// Ratings with no reviews are absent from the result.
    /// </summary>
    Task<IReadOnlyDictionary<int, int>> GetRatingDistributionAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Average rating and review count per calendar month across the given courses, for months that
    /// have at least one review. Ordered oldest-first is the caller's concern.
    /// </summary>
    Task<IReadOnlyList<MonthlyRatingBucket>> GetMonthlyRatingTrendAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);
}

public sealed record MonthlyRatingBucket(int Year, int Month, double Average, int Count);
