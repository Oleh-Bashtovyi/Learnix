using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.Payments.Specifications;

public sealed class InstructorPaymentsSpecification : Specification<Payment>
{
    public InstructorPaymentsSpecification(Guid instructorId)
    {
        Query.Where(p => p.Status == PaymentStatus.Completed &&
                         p.Course != null &&
                         p.Course.InstructorId == instructorId);
        Query.Include(p => p.Course);
        Query.OrderByDescending(p => p.CreatedAt);
        Query.AsNoTracking();
    }
}
