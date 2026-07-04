using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Users.Specifications;

public sealed class AdminUserListSpecification : Specification<User>
{
    public AdminUserListSpecification(string? search, int skip, int take, bool includeDeleted)
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

        Query.OrderByDescending(u => u.CreatedAt)
             .Skip(skip)
             .Take(take)
             .AsNoTracking();
    }
}
