using FluentResults;
using Learnix.Application.Common.Caching;
using Learnix.Application.Common.Constants;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetCourseById;

public sealed record GetCourseByIdQuery(Guid CourseId)
    : IRequest<Result<CourseDetailDto>>, ICacheable<CourseDetailDto>
{
    public string CacheKey => CacheKeys.Course(CourseId);
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
