using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.Courses.Specifications;

public sealed class AdminCoursesByStatusCountSpecification : Specification<Course>
{
    public AdminCoursesByStatusCountSpecification(CourseStatus status)
    {
        Query
            .IgnoreQueryFilters()
            .Where(c => c.Status == status && !c.IsDeleted);
    }
}
