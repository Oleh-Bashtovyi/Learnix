using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Reviews.Specifications;

public sealed class CourseReviewsByCoursePaginatedSpecification : Specification<CourseReview>
{
    public CourseReviewsByCoursePaginatedSpecification(Guid courseId, int skip, int take)
    {
        Query
            .Where(r => r.CourseId == courseId)
            .Include(r => r.Student!)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .AsNoTracking();
    }
}
