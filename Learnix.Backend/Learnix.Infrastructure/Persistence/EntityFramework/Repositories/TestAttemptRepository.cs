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

        return await context.TestAttempts
            .Where(a => courseIds.Contains(a.CourseId) && a.SubmittedAt.HasValue)
            .GroupBy(a => new { a.CourseId, a.TestLessonId })
            .Select(g => new TestPerformanceBucket(
                g.Key.CourseId,
                g.Key.TestLessonId,
                g.Count(),
                g.Average(a => (double)(a.Score ?? 0)),
                g.Max(a => a.MaxScore ?? 0),
                g.Count(a => a.Passed == true)))
            .ToListAsync(cancellationToken);
    }
}
