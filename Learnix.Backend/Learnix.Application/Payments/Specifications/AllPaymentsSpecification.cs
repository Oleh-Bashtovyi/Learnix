using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Payments.Specifications;

public sealed class AllPaymentsSpecification : Specification<Payment>
{
    public AllPaymentsSpecification(string? search, int skip, int take)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            Query.Where(p =>
                (p.User != null && p.User.Email!.ToLower().Contains(lower)) ||
                (p.Course != null && p.Course.Title.ToLower().Contains(lower)));
        }

        Query.Include(p => p.Course);
        Query.Include(p => p.User);
        Query.OrderByDescending(p => p.CreatedAt);
        Query.Skip(skip).Take(take);
        Query.AsNoTracking();
    }
}
