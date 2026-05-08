using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Certificates.Specifications;

public sealed class CertificatesByStudentSpecification : Specification<Certificate>
{
    public CertificatesByStudentSpecification(Guid studentId)
    {
        Query.Where(c => c.StudentId == studentId);
        Query.Include(c => c.Course);
        Query.OrderByDescending(c => c.IssuedAt);
        Query.AsNoTracking();
    }
}
