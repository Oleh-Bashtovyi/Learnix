using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CategoryBySlugSpecification : Specification<Category>, ISingleResultSpecification<Category>
{
    public CategoryBySlugSpecification(string slug, bool forUpdate = false)
    {
        Query.Where(c => c.Slug == slug);

        if (!forUpdate)
        {
            Query.AsNoTracking();
        }
    }
}
