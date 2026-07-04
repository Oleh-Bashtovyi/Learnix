using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Users.Specifications;

public sealed class UserByIdSpecification : Specification<User>, ISingleResultSpecification<User>
{
    public UserByIdSpecification(Guid id, bool forUpdate = false)
    {
        Query.Where(u => u.Id == id);

        if (!forUpdate)
            Query.AsNoTracking();
    }
}
