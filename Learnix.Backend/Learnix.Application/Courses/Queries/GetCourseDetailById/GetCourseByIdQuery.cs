using FluentResults;
using Learnix.Application.Common.Caching;
using Learnix.Application.Common.Constants;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetCourseById;

public sealed record GetCourseByIdQuery(Guid CourseId)
    : IRequest<Result<CourseDetailDto>>, ICacheable<CourseDetailDto>
{
    public string CacheKey => CacheKeys.Courses.ById(CourseId);
    public TimeSpan Expiration => CacheKeys.Courses.ByIdTtl;
}
