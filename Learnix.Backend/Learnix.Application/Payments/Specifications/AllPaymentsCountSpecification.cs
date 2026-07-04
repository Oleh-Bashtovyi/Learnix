using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Payments.Specifications;

public sealed class AllPaymentsCountSpecification : Specification<Payment>
{
    public AllPaymentsCountSpecification(string? search)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            Query.Where(p =>
                (p.User != null && p.User.Email!.ToLower().Contains(lower)) ||
                (p.Course != null && p.Course.Title.ToLower().Contains(lower)));
        }
    }
}
