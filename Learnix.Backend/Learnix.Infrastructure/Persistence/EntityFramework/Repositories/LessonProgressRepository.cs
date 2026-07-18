using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class LessonProgressRepository(ApplicationDbContext context)
    : RepositoryBase<LessonProgressEntity>(context), ILessonProgressRepository
{
    public void Add(LessonProgressEntity progress) => context.Set<LessonProgressEntity>().Add(progress);

    public async Task<IReadOnlyDictionary<Guid, CourseProgressCounts>> GetProgressCountsAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default)
    {
        if (courseIds.Count == 0)
            return new Dictionary<Guid, CourseProgressCounts>();

        var ids = courseIds.Distinct().ToArray();

        var totals = await (
            from lesson in context.Set<Lesson>()
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where ids.Contains(section.CourseId) && !lesson.IsHidden
            group lesson by section.CourseId into grouped
            select new { CourseId = grouped.Key, Count = grouped.Count() }
        ).ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);

        var completed = await (
            from lp in context.Set<LessonProgressEntity>()
            join lesson in context.Set<Lesson>() on lp.LessonId equals lesson.Id
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where lp.StudentId == studentId && ids.Contains(section.CourseId)
                  && lp.IsCompleted && !lesson.IsHidden
            group lp by section.CourseId into grouped
            select new { CourseId = grouped.Key, Count = grouped.Count() }
        ).ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);

        return ids.ToDictionary(
            id => id,
            id => new CourseProgressCounts(
                completed.GetValueOrDefault(id),
                totals.GetValueOrDefault(id)));
    }

    public async Task<IReadOnlyDictionary<Guid, DateTime>> GetLastActivityByCourseAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default)
    {
        if (courseIds.Count == 0)
            return new Dictionary<Guid, DateTime>();

        var ids = courseIds.Distinct().ToArray();

        return await (
            from lp in context.Set<LessonProgressEntity>()
            where lp.StudentId == studentId && ids.Contains(lp.CourseId)
            group lp by lp.CourseId into grouped
            select new { CourseId = grouped.Key, LastAccessedAt = grouped.Max(x => x.LastAccessedAt) }
        ).ToDictionaryAsync(x => x.CourseId, x => x.LastAccessedAt, cancellationToken);
    }

    public async Task<int> CountStartedEnrollmentsAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default)
    {
        if (courseIds.Count == 0)
            return 0;

        return await context.Set<LessonProgressEntity>()
            .Where(lp => courseIds.Contains(lp.CourseId) && lp.IsCompleted)
            .Select(lp => new { lp.StudentId, lp.CourseId })
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountActiveStudentsSinceAsync(
        IReadOnlyCollection<Guid> courseIds,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        if (courseIds.Count == 0)
            return 0;

        return await context.Set<LessonProgressEntity>()
            .Where(lp => courseIds.Contains(lp.CourseId) && lp.LastAccessedAt >= since)
            .Select(lp => lp.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetCompletedCountByLessonAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var counts = await context.Set<LessonProgressEntity>()
            .Where(lp => lp.CourseId == courseId && lp.IsCompleted)
            .GroupBy(lp => lp.LessonId)
            .Select(g => new { LessonId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.LessonId, x => x.Count);
    }
}
