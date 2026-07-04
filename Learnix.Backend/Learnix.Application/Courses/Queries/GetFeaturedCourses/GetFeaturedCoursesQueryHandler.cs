using FluentResults;
using Learnix.Application.Courses.Abstractions;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetFeaturedCourses;

internal sealed class GetFeaturedCoursesQueryHandler(IFeaturedCoursesService featuredService)
    : IRequestHandler<GetFeaturedCoursesQuery, Result<IReadOnlyList<FeaturedCourseDto>>>
{
    private const int FeaturedCount = 6;

    public async Task<Result<IReadOnlyList<FeaturedCourseDto>>> Handle(
        GetFeaturedCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var courses = await featuredService.GetTopFeaturedAsync(FeaturedCount, cancellationToken);
        return Result.Ok(courses);
    }
}
