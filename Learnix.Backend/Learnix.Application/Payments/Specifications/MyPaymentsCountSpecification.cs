using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Payments.Specifications;

public sealed class MyPaymentsCountSpecification : Specification<Payment>
{
    public MyPaymentsCountSpecification(Guid userId)
    {
        Query.Where(p => p.UserId == userId);
    }
}
