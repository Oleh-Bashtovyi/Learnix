using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.Courses.Specifications;

/// <summary>
/// An instructor's published courses, most popular first.
/// <para>
/// Published only: a draft is not something to show the public or the AI, and an archived course is
/// not something its instructor should still be judged on.
/// </para>
/// <para>
/// Lives here rather than in a feature folder because two features ask the same question — the AI tool
/// that lists an instructor's courses, and the aggregates on their public profile. `take` is null for
/// the profile, which has to count all of them or its totals would be a lie.
/// </para>
/// </summary>
public sealed class PublishedCoursesByInstructorSpecification : Specification<Course>
{
    public PublishedCoursesByInstructorSpecification(Guid instructorId, int? take = null)
    {
        Query
            .Where(c => c.InstructorId == instructorId && c.Status == CourseStatus.Published)
            .OrderByDescending(c => c.EnrollmentsCount)
            .ThenByDescending(c => c.UpdatedAt)
            .AsNoTracking();

        if (take.HasValue)
            Query.Take(take.Value);
    }
}
