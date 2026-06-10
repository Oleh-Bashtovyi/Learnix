using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Certificates.Specifications;

public sealed class CertificateByCodeSpecification : Specification<Certificate>, ISingleResultSpecification<Certificate>
{
    public CertificateByCodeSpecification(string code)
    {
        Query.Where(c => c.Code == code)
             .Include(c => c.Course)
             .AsNoTracking();
    }
}
