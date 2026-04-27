using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.Enrollments.Specifications;

public sealed class ActiveEnrollmentByStudentAndCourseSpecification
    : Specification<Enrollment>, ISingleResultSpecification<Enrollment>
{
    public ActiveEnrollmentByStudentAndCourseSpecification(Guid studentId, Guid courseId)
    {
        Query.Where(e =>
            e.StudentId == studentId &&
            e.CourseId == courseId &&
            e.Status == EnrollmentStatus.Active &&
            e.PaymentStatus == PaymentStatus.Completed);

        Query.AsNoTracking();
    }
}
