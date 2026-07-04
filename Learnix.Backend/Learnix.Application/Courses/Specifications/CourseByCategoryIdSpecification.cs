using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CourseByCategoryIdSpecification : Specification<Course>
{
    public CourseByCategoryIdSpecification(Guid categoryId)
    {
        Query
            .Where(c => c.CategoryId == categoryId)
            .AsNoTracking();
    }
}
