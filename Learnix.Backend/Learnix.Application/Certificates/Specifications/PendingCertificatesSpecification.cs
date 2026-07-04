using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Certificates.Specifications;

public sealed class PendingCertificatesSpecification : Specification<Certificate>
{
    public PendingCertificatesSpecification()
    {
        Query.Where(c => c.FileUrl == null);
        Query.Take(50);
    }
}
