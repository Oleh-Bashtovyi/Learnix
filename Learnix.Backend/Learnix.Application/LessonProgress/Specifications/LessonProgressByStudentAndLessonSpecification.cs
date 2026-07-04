using Ardalis.Specification;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.LessonProgress.Specifications;

public sealed class LessonProgressByStudentAndLessonSpecification
    : Specification<LessonProgressEntity>, ISingleResultSpecification<LessonProgressEntity>
{
    public LessonProgressByStudentAndLessonSpecification(Guid studentId, Guid lessonId, bool forUpdate = false)
    {
        Query.Where(lp => lp.StudentId == studentId && lp.LessonId == lessonId);

        if (!forUpdate)
            Query.AsNoTracking();
    }
}
