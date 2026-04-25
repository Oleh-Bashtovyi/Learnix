using Learnix.Domain.Common;

namespace Learnix.Domain.Entities;

public class LessonProgress : BaseEntity
{
    private LessonProgress() { }

    private LessonProgress(Guid courseId, Guid lessonId, Guid studentId)
    {
        CourseId = courseId;
        LessonId = lessonId;
        StudentId = studentId;
        LastAccessedAt = DateTime.UtcNow;
    }

    public Guid CourseId { get; private set; }
    public Guid LessonId { get; private set; }
    public Guid StudentId { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime LastAccessedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public static LessonProgress Create(Guid courseId, Guid lessonId, Guid studentId)
        => new(courseId, lessonId, studentId);

    public void Touch()
        => LastAccessedAt = DateTime.UtcNow;

    public void MarkCompleted()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        LastAccessedAt = DateTime.UtcNow;
    }

    public void Reset()
    {
        IsCompleted = false;
        CompletedAt = null;
        LastAccessedAt = DateTime.UtcNow;
    }
}
