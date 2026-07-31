using FluentResults;
using Learnix.Application.Common.Caching;
using Learnix.Application.Common.Constants;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetPublishedCourseCount;

public sealed record GetPublishedCourseCountQuery() : IRequest<Result<int>>, ICacheable<int>
{
    public string CacheKey => CacheKeys.Courses.PublishedCount;
    public TimeSpan Expiration => CacheKeys.Courses.PublishedCountTtl;
}
