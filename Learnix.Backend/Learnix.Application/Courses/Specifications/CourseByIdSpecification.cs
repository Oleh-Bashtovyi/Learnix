using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CourseByIdSpecification : Specification<Course>, ISingleResultSpecification<Course>
{
    public CourseByIdSpecification(
        Guid id,
        bool includeSections = false,
        bool includeLessons = false,
        Guid? sectionIdForLessons = null,
        bool forUpdate = false)
    {
        Query.Where(c => c.Id == id);

        if (includeSections)
        {
            if (sectionIdForLessons.HasValue)
            {
                var sid = sectionIdForLessons.Value;
                Query.Include(c => c.Sections)
                    .ThenInclude(s => s.Lessons.Where(l => l.SectionId == sid));
            }
            else if (includeLessons)
            {
                Query.Include(c => c.Sections)
                    .ThenInclude(s => s.Lessons);
            }
            else
            {
                Query.Include(c => c.Sections);
            }
        }

        if (!forUpdate)
        {
            Query.AsNoTracking();
        }
    }
}
