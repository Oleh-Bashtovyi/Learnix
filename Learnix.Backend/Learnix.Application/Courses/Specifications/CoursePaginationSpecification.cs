using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CoursePaginationSpecification : Specification<Course>
{
    public CoursePaginationSpecification(int skip, int take)
    {
        Query
            .Skip(skip)
            .Take(take)
            .AsNoTracking();
    }
}
