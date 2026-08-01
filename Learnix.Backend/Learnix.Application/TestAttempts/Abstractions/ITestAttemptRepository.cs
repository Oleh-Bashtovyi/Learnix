using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.TestAttempts.Abstractions;

public interface ITestAttemptRepository : IRepositoryBase<TestAttempt>
{
    /// <summary>
    /// Score/pass-rate stats per (course, test lesson) across the given courses.
    /// </summary>
    Task<IReadOnlyList<TestPerformanceBucket>> GetPerformanceByTestAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);
}

public sealed record TestPerformanceBucket(
    Guid CourseId,
    Guid TestLessonId,
    int TotalAttempts,
    double AverageScore,
    int MaxScore,
    int PassedCount);
