using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Abstractions;

public interface ILessonRepository : IRepositoryBase<Lesson>
{
    Task<int> GetMaxDisplayOrderAsync(Guid sectionId, CancellationToken ct = default);
    Task ShiftLessonsOrderAsync(Guid sectionId, Guid lessonId, int newOrder, CancellationToken ct);
    Task<T?> GetLessonOfTypeByIdAsync<T>(Guid id, bool forUpdate = false, CancellationToken ct = default) where T : Lesson;

    /// <summary>
    /// Returns true when the lesson is visible (not hidden) and belongs to a section of the given course.
    /// </summary>
    Task<bool> IsLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken ct = default);

    /// <summary>
    /// Returns the TestLesson if it is visible and belongs to a section of the given course, otherwise null.
    /// </summary>
    Task<TestLesson?> GetTestLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken ct = default);

    /// <summary>
    /// Returns the count of visible (non-hidden) lessons across all sections of the given course.
    /// </summary>
    Task<int> GetVisibleLessonCountAsync(Guid courseId, CancellationToken ct = default);

    /// <summary>
    /// Returns the count of completed visible lessons for a student in a given course.
    /// </summary>
    Task<int> GetCompletedVisibleLessonCountAsync(Guid studentId, Guid courseId, CancellationToken ct = default);

    /// <summary>
    /// Returns a visible lesson that belongs to the given course, preserving the derived EF type
    /// (VideoLesson / PostLesson / TestLesson) so the caller can pattern-match on it.
    /// </summary>
    Task<Lesson?> GetVisibleLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken ct = default);
}
