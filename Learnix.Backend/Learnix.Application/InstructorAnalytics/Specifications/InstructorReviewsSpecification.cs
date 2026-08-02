using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.InstructorAnalytics.Specifications;

public sealed class InstructorReviewsSpecification : Specification<CourseReview>
{
    public InstructorReviewsSpecification(List<Guid> courseIds, int? take = null)
    {
        Query.Where(r => courseIds.Contains(r.CourseId));
        Query.Include(r => r.Student);
        Query.OrderByDescending(r => r.CreatedAt);

        // Take the newest N in the database rather than loading every review and trimming in memory.
        if (take is > 0)
            Query.Take(take.Value);

        Query.AsNoTracking();
    }
}
