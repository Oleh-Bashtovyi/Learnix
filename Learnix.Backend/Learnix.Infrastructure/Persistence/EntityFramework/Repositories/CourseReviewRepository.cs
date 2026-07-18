using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class CourseReviewRepository(ApplicationDbContext context)
    : RepositoryBase<CourseReview>(context), ICourseReviewRepository
{
    public async Task<(int Count, decimal Average)> GetCourseRatingMetricsAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var stats = await context.CourseReviews
            .Where(r => r.CourseId == courseId)
            .GroupBy(r => r.CourseId)
            .Select(g => new { Count = g.Count(), Average = g.Average(r => (decimal)r.Rating) })
            .FirstOrDefaultAsync(cancellationToken);

        return stats == null ? (0, 0m) : (stats.Count, Math.Round(stats.Average, 2));
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetMyRatingsAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default)
    {
        if (courseIds.Count == 0)
            return new Dictionary<Guid, int>();

        var ratings = await context.CourseReviews
            .Where(r => r.StudentId == studentId && courseIds.Contains(r.CourseId))
            .Select(r => new { r.CourseId, r.Rating })
            .ToListAsync(cancellationToken);

        return ratings.ToDictionary(r => r.CourseId, r => r.Rating);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetRatingDistributionAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default)
    {
        if (courseIds.Count == 0)
            return new Dictionary<int, int>();

        var buckets = await context.CourseReviews
            .Where(r => courseIds.Contains(r.CourseId))
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return buckets.ToDictionary(b => b.Rating, b => b.Count);
    }

    public async Task<IReadOnlyList<MonthlyRatingBucket>> GetMonthlyRatingTrendAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default)
    {
        if (courseIds.Count == 0)
            return [];

        var buckets = await context.CourseReviews
            .Where(r => courseIds.Contains(r.CourseId))
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .Select(g => new MonthlyRatingBucket(
                g.Key.Year,
                g.Key.Month,
                g.Average(r => (double)r.Rating),
                g.Count()))
            .ToListAsync(cancellationToken);

        return buckets;
    }
}
