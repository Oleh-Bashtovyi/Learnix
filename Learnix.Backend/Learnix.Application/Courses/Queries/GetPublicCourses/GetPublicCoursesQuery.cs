using FluentResults;
using Learnix.Application.Common.Caching;
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
    // Key includes all filter params — each unique combination gets its own entry.
    // No explicit invalidation: short TTL (5 min) is sufficient for catalog pages.
    public string CacheKey =>
        $"courses:public:{Search}:{Skip}:{Take}:{CategoryId}:{InstructorId}:{SortBy}:{IsFree}:{MinRating}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
