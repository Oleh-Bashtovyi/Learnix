using FluentResults;
using Learnix.Application.Common.Caching;
using Learnix.Application.Common.Constants;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetFeaturedCourses;

public sealed record GetFeaturedCoursesQuery()
    : IRequest<Result<IReadOnlyList<FeaturedCourseDto>>>, ICacheable<IReadOnlyList<FeaturedCourseDto>>
{
    public string CacheKey => CacheKeys.CoursesFeatured;
    public TimeSpan Expiration => TimeSpan.FromMinutes(30);
}
