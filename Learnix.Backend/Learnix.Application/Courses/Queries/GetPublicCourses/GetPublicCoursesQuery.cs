using FluentResults;
using Learnix.Application.Common.Caching;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetPublicCourses;

public sealed record GetPublicCoursesQuery(
    string? Search,
    int Skip,
    int Take,
    Guid? CategoryId,
    Guid? InstructorId,
    string? SortBy,
    bool? IsFree,
    decimal? MinRating) : IRequest<Result<PaginatedResult<PublicCourseCardDto>>>, ICacheable<PaginatedResult<PublicCourseCardDto>>
{
    /// <summary>
    /// Built from the <em>normalized</em> inputs, so that any two requests the database cannot
    /// tell apart share a cache entry. Without this, <c>?skip=0</c> and <c>?skip=7</c> (take=20)
    /// return the same page under two keys, as do <c>?search=React</c> and <c>?search=react</c>.
    /// </summary>
    public string CacheKey
    {
        get
        {
            var page = PaginationRequest.FromOffset(Skip, Take);
            var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim().ToLowerInvariant();

            return CacheKeys.Courses.Public(
                search, page.PageIndex, page.PageSize, CategoryId, InstructorId, SortBy, IsFree, MinRating);
        }
    }

    public TimeSpan Expiration => CacheKeys.Courses.PublicTtl;
}
