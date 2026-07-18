using Ardalis.Specification;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.LessonProgress.Abstractions;

public interface ILessonProgressRepository : IRepositoryBase<LessonProgressEntity>
{
    /// <summary>
    /// Stages the progress row without saving. Unlike <c>AddAsync</c>, which commits on its own, this
    /// leaves the unit of work to the caller — so completing a lesson and everything it triggers
    /// (course completion, certificate) commit together or not at all.
    /// </summary>
    void Add(LessonProgressEntity progress);

    /// <summary>
    /// Returns completed and total visible lesson counts for the student in each of the given courses.
    /// Every requested course id is present in the result, even when the course has no visible lessons.
    /// </summary>
    /// <remarks>
    /// Visibility lives on <c>Lesson.IsHidden</c>, and <c>LessonProgress</c> has no navigation to <c>Lesson</c>,
    /// so this cannot be expressed as a <see cref="Specification{T}"/> and is a custom method instead.
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, CourseProgressCounts>> GetProgressCountsAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The most recent <c>LastAccessedAt</c> the student has in each of the given courses.
    /// Courses the student has no progress rows in are absent from the result.
    /// </summary>
    /// <remarks>
    /// Progress rows are only written when a lesson is completed, so this timestamp marks the last
    /// lesson the student <i>finished</i> in the course, not the last one they opened.
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, DateTime>> GetLastActivityByCourseAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// How many (student, course) enrollments have completed at least one lesson in the given courses —
    /// the "started learning" stage of the engagement funnel.
    /// </summary>
    Task<int> CountStartedEnrollmentsAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Distinct students with any lesson activity in the given courses since <paramref name="since"/>.
    /// </summary>
    Task<int> CountActiveStudentsSinceAsync(
        IReadOnlyCollection<Guid> courseIds,
        DateTime since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// How many students have completed each lesson of the course, keyed by lesson id. The unique
    /// <c>(StudentId, LessonId)</c> index means one completed row per student per lesson, so a plain
    /// COUNT is already the distinct-student count. Lessons nobody has completed are absent.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetCompletedCountByLessonAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);
}
