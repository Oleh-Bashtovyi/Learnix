using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Reviews.Specifications;

public sealed class CourseReviewsByCoursePaginatedCountSpecification : Specification<CourseReview>
{
    public CourseReviewsByCoursePaginatedCountSpecification(Guid courseId)
    {
        Query.Where(r => r.CourseId == courseId);
    }
}
