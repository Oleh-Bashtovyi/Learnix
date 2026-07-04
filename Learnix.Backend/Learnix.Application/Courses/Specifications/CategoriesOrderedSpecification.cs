using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CategoriesOrderedSpecification : Specification<Category>
{
    public CategoriesOrderedSpecification()
    {
        Query
            .OrderBy(c => c.Name)
            .AsNoTracking();
    }
}
