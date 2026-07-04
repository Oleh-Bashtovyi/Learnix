using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Payments.Specifications;

public sealed class MyPaymentsSpecification : Specification<Payment>
{
    public MyPaymentsSpecification(Guid userId, int skip, int take)
    {
        Query.Where(p => p.UserId == userId);
        Query.Include(p => p.Course);
        Query.OrderByDescending(p => p.CreatedAt);
        Query.Skip(skip).Take(take);
        Query.AsNoTracking();
    }
}
