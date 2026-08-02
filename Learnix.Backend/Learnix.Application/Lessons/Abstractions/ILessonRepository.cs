using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Abstractions;

public interface ILessonRepository : IRepositoryBase<Lesson>
{
    Task<T?> GetLessonOfTypeByIdAsync<T>(Guid id, bool forUpdate = false, CancellationToken cancellationToken = default) where T : Lesson;

    /// <summary>
    /// Returns true when the lesson is visible (not hidden) and belongs to a section of the given course.
    /// </summary>
    Task<bool> IsLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the TestLesson if it is visible and belongs to a section of the given course, otherwise null.
    /// </summary>
    Task<TestLesson?> GetTestLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every visible (non-hidden) lesson of the course, each flagged with whether the student has
    /// completed it — no aggregate; the caller decides what "finished" means.
    /// </summary>
    Task<IReadOnlyList<LessonCompletion>> GetVisibleLessonCompletionAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a visible lesson that belongs to the given course, preserving the derived EF type
    /// (VideoLesson / PostLesson / TestLesson) so the caller can pattern-match on it.
    /// </summary>
    Task<Lesson?> GetVisibleLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The lesson a student should land on when resuming the course: the first visible lesson they have
    /// not completed, in section then lesson order. Falls back to the first visible lesson when every
    /// lesson is already complete. Null when the course has no visible lessons.
    /// </summary>
    Task<Guid?> GetResumeLessonIdAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
}

public sealed record LessonCompletion(Guid LessonId, bool IsCompleted);
