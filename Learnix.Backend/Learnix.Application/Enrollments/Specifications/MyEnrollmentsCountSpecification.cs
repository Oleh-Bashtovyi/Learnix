using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Enrollments.Specifications;

public sealed class MyEnrollmentsCountSpecification : Specification<Enrollment>
{
    public MyEnrollmentsCountSpecification(Guid studentId)
    {
        Query.Where(e => e.StudentId == studentId);
    }
}
