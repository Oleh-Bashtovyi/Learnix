using Ardalis.Specification;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.LessonProgress.Specifications;

public sealed class CompletedLessonCountByStudentAndCourseSpecification : Specification<LessonProgressEntity>
{
    public CompletedLessonCountByStudentAndCourseSpecification(Guid studentId, Guid courseId)
    {
        Query.Where(lp => lp.StudentId == studentId && lp.CourseId == courseId && lp.IsCompleted);
    }
}
