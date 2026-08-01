using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Specifications;

public sealed class TestVersionByIdSpecification
    : Specification<TestVersion>, ISingleResultSpecification<TestVersion>
{
    public TestVersionByIdSpecification(Guid versionId, bool forUpdate = false)
    {
        Query.Where(v => v.Id == versionId);

        if (!forUpdate)
            Query.AsNoTracking();
    }
}
