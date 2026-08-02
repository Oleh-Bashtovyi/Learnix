using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.InstructorAnalytics.Specifications;

public sealed class InstructorCoursesForAnalyticsSpecification : Specification<Course>
{
    public InstructorCoursesForAnalyticsSpecification(
        Guid instructorId,
        bool includeSections = false,
        IReadOnlyCollection<Guid>? courseIds = null)
    {
        Query.Where(c => c.InstructorId == instructorId);

        if (courseIds is not null)
            Query.Where(c => courseIds.Contains(c.Id));

        if (includeSections)
            Query.Include(c => c.Sections).ThenInclude(s => s.Lessons);

        Query.AsNoTracking();
    }
}
