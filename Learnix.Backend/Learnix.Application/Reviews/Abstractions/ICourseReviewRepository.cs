using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Reviews.Abstractions;

public interface ICourseReviewRepository : IRepositoryBase<CourseReview>
{
    Task<(int Count, decimal Average)> GetCourseRatingMetricsAsync(Guid courseId, CancellationToken ct = default);
}
