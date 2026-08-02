using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.TestAttempts.Specifications;

/// <summary>
/// Any attempt at all — in progress or submitted — pinned to a given version. It is the question
/// "does anyone still need these questions the way they are?", asked before an edit decides whether
/// to overwrite the version or branch a new one.
/// <para>
/// In-progress attempts count: a student who has the questions on screen is exactly who overwriting
/// them would hurt.
/// </para>
/// </summary>
public sealed class AttemptsByTestVersionSpecification : Specification<TestAttempt>
{
    public AttemptsByTestVersionSpecification(Guid testVersionId)
    {
        Query.Where(a => a.TestVersionId == testVersionId).AsNoTracking();
    }
}
