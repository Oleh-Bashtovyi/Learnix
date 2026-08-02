using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.InstructorAnalytics.Specifications;

/// <summary>
/// The instructor's certificates issued within a window, inclusive of both ends.
/// </summary>
public sealed class InstructorCertificatesInPeriodSpecification : Specification<Certificate>
{
    public InstructorCertificatesInPeriodSpecification(Guid instructorId, DateTime startUtc, DateTime endUtc)
    {
        Query.Where(c => c.Course != null
                         && c.Course.InstructorId == instructorId
                         && c.IssuedAt >= startUtc
                         && c.IssuedAt <= endUtc);
        Query.AsNoTracking();
    }
}
