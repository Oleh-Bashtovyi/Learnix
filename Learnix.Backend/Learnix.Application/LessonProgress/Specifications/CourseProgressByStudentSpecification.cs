using Ardalis.Specification;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.LessonProgress.Specifications;

public sealed class CourseProgressByStudentSpecification : Specification<LessonProgressEntity>
{
    public CourseProgressByStudentSpecification(Guid studentId, Guid courseId)
    {
        Query.Where(lp => lp.StudentId == studentId && lp.CourseId == courseId);
        Query.AsNoTracking();
    }
}
