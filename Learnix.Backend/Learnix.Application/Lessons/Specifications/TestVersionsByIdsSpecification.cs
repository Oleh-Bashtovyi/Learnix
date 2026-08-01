using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Specifications;

/// <summary>
/// The versions behind a batch of test lessons, for callers that project a whole course at once and
/// would otherwise issue a query per test.
/// </summary>
public sealed class TestVersionsByIdsSpecification : Specification<TestVersion>
{
    public TestVersionsByIdsSpecification(IReadOnlyCollection<Guid> versionIds)
    {
        Query.Where(v => versionIds.Contains(v.Id)).AsNoTracking();
    }
}
