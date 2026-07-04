using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.AiChat.Specifications;

public sealed class CategoriesByIdsSpecification : Specification<Category>
{
    public CategoriesByIdsSpecification(IReadOnlyList<Guid> ids)
    {
        Query.Where(c => ids.Contains(c.Id)).AsNoTracking();
    }
}
