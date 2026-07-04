using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Certificates.Specifications;

public sealed class CertificateByCourseAndStudentSpecification
    : Specification<Certificate>, ISingleResultSpecification<Certificate>
{
    public CertificateByCourseAndStudentSpecification(Guid studentId, Guid courseId, bool forUpdate = false)
    {
        Query.Where(c => c.StudentId == studentId && c.CourseId == courseId);

        if (!forUpdate)
        {
            Query.AsNoTracking();
        }
    }
}
