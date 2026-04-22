using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Specification;

public sealed class LessonByIdSpecification : Specification<Lesson>, ISingleResultSpecification<Lesson>
{
    public LessonByIdSpecification(Guid lessonId, bool forUpdate = false)
    {
        Query.Where(l => l.Id == lessonId);

        if (!forUpdate)
        {
            Query.AsNoTracking();
        }
    }
}

public sealed class PostLessonByIdSpecification : Specification<Lesson>, ISingleResultSpecification<Lesson>
{
    public PostLessonByIdSpecification(Guid lessonId, bool forUpdate = false)
    {
        Query.Where(l => l.Id == lessonId && l.LessonType == Domain.Enums.LessonType.Post);

        if (!forUpdate)
        {
            Query.AsNoTracking();
        }
    }
}
