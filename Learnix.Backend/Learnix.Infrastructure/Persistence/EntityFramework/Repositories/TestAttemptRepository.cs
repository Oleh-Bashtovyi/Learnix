using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class TestAttemptRepository(ApplicationDbContext context)
    : RepositoryBase<TestAttempt>(context), ITestAttemptRepository
{
    public async Task<IReadOnlyList<TestPerformanceBucket>> GetPerformanceByTestAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default)
    {
        if (courseIds.Count == 0)
            return [];

        // Restricted to each test's current version — see ADR-BACK-LMS-006.
        return await (
            from a in context.TestAttempts
            join tl in context.Set<TestLesson>() on a.TestLessonId equals tl.Id
            where courseIds.Contains(a.CourseId) && a.SubmittedAt.HasValue && a.TestVersionId == tl.CurrentVersionId
            group a by new { a.CourseId, a.TestLessonId } into g
            select new TestPerformanceBucket(
                g.Key.CourseId,
                g.Key.TestLessonId,
                g.Count(),
                g.Average(a => (double)(a.Score ?? 0)),
                g.Max(a => a.MaxScore ?? 0),
                g.Count(a => a.Passed == true)))
            .ToListAsync(cancellationToken);
    }
}
