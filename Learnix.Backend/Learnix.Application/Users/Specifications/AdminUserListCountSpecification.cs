using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Users.Specifications;

public sealed class AdminUserListCountSpecification : Specification<User>
{
    public AdminUserListCountSpecification(string? search, bool includeDeleted)
    {
        if (includeDeleted)
        {
            Query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();
            Query.Where(u =>
                u.Email!.ToLower().Contains(normalized) ||
                u.FirstName.ToLower().Contains(normalized) ||
                u.LastName.ToLower().Contains(normalized));
        }
    }
}
