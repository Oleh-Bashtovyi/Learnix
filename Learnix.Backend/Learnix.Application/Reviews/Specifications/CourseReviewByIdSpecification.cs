using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Reviews.Specifications;

public sealed class CourseReviewByIdSpecification : Specification<CourseReview>, ISingleResultSpecification<CourseReview>
{
    public CourseReviewByIdSpecification(Guid id, bool forUpdate = false)
    {
        Query.Where(r => r.Id == id);
        if (!forUpdate) Query.AsNoTracking();
    }
}
