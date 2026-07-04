using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Enrollments.Specifications;

public sealed class MyEnrollmentsSpecification : Specification<Enrollment>
{
    public MyEnrollmentsSpecification(Guid studentId, int skip, int take)
    {
        Query.Where(e => e.StudentId == studentId);
        Query.Include(e => e.Course);
        Query.OrderByDescending(e => e.EnrolledAt);
        Query.Skip(skip).Take(take);
        Query.AsNoTracking();
    }
}
