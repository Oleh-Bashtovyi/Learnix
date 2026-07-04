using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CategoryByIdSpecification : Specification<Category>, ISingleResultSpecification<Category>
{
    public CategoryByIdSpecification(Guid id, bool forUpdate = false)
    {
        Query.Where(c => c.Id == id);

        if (!forUpdate)
        {
            Query.AsNoTracking();
        }
    }
}
