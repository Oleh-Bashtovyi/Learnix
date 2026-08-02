using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Abstractions;

public interface ITestVersionRepository : IRepositoryBase<TestVersion>
{
    /// <summary>
    /// Stages the version without saving. Unlike <c>AddAsync</c>, which commits on its own, this leaves
    /// the unit of work to the caller — and here that is not a preference but a requirement: a version
    /// has a foreign key to its lesson, so committing it before the lesson row exists violates it. Both
    /// go in on the same <c>SaveChangesAsync</c>, in the order EF works out.
    /// </summary>
    void Add(TestVersion version);
}
