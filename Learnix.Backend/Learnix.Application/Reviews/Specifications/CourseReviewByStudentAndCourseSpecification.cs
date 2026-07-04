using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Reviews.Specifications;

public sealed class CourseReviewByStudentAndCourseSpecification : Specification<CourseReview>, ISingleResultSpecification<CourseReview>
{
    public CourseReviewByStudentAndCourseSpecification(Guid studentId, Guid courseId, bool forUpdate = false)
    {
        Query.Where(r => r.StudentId == studentId && r.CourseId == courseId);
        if (!forUpdate) Query.AsNoTracking();
    }
}
