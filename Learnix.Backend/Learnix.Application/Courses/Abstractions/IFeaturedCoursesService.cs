using Learnix.Application.Courses.Queries.GetFeaturedCourses;

namespace Learnix.Application.Courses.Abstractions;

public interface IFeaturedCoursesService
{
    Task<IReadOnlyList<FeaturedCourseDto>> GetTopFeaturedAsync(int count, CancellationToken ct);
}
