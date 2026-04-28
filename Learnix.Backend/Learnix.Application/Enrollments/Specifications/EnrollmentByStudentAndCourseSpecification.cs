using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Enrollments.Specifications;

public sealed class EnrollmentByStudentAndCourseSpecification
    : Specification<Enrollment>, ISingleResultSpecification<Enrollment>
{
    public EnrollmentByStudentAndCourseSpecification(
        Guid studentId,
        Guid courseId,
        bool forUpdate = false)
    {
        Query.Where(e => e.StudentId == studentId && e.CourseId == courseId);

        if (!forUpdate)
        {
            Query.AsNoTracking();
        }
    }
}
