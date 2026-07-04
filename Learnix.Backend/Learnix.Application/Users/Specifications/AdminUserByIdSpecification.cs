using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Users.Specifications;

public sealed class AdminUserByIdSpecification : Specification<User>, ISingleResultSpecification<User>
{
    public AdminUserByIdSpecification(Guid id, bool includeDeleted = false, bool forUpdate = false)
    {
        Query.Where(u => u.Id == id);

        if (includeDeleted)
            Query.IgnoreQueryFilters();

        if (!forUpdate)
            Query.AsNoTracking();
    }
}
