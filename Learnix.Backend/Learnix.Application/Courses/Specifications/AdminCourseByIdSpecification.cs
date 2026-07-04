using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class AdminCourseByIdSpecification : Specification<Course>, ISingleResultSpecification<Course>
{
    public AdminCourseByIdSpecification(Guid id, bool forUpdate = false)
    {
        Query.Where(c => c.Id == id).IgnoreQueryFilters();

        if (!forUpdate)
            Query.AsNoTracking();
    }
}
