using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Users.Specifications;

public sealed class UsersByIdsSpecification : Specification<User>
{
    public UsersByIdsSpecification(IReadOnlyCollection<Guid> ids)
    {
        Query.Where(u => ids.Contains(u.Id));
        Query.AsNoTracking();
    }
}
