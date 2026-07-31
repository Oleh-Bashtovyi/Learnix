using FluentResults;
using Learnix.Application.Common.Caching;
using Learnix.Application.Common.Constants;
using MediatR;

namespace Learnix.Application.AiChat.Queries.SearchCourses;

public sealed record SearchCoursesQuery(
    string Query,
    string? Category = null,
    int MaxResults = 10)
    : IRequest<Result<IReadOnlyList<CourseSearchResultDto>>>, ICacheable<IReadOnlyList<CourseSearchResultDto>>
{
    public string CacheKey => CacheKeys.AiChat.CourseSearch(
        Query.Trim().ToLowerInvariant(),
        string.IsNullOrWhiteSpace(Category) ? null : Category.Trim().ToLowerInvariant(),
        Math.Clamp(MaxResults, 1, 20));

    public TimeSpan Expiration => CacheKeys.AiChat.CourseSearchTtl;
}
