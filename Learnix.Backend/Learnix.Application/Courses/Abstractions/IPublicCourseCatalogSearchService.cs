using Learnix.Application.Common.Pagination;
using Learnix.Application.Courses.Queries.GetPublicCourses;

namespace Learnix.Application.Courses.Abstractions;

public interface IPublicCourseCatalogSearchService
{
    Task<PaginatedResult<PublicCourseCardDto>> SearchAsync(
        string? search,
        PaginationRequest pagination,
        Guid? categoryId,
        Guid? instructorId,
        string? sortBy,
        bool? isFree,
        decimal? minRating,
        CancellationToken ct);
}
