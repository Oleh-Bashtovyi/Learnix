using Learnix.Application.Common.Pagination;
using Learnix.Application.Courses.Queries.GetPublicCourses;

namespace Learnix.Application.Courses.Abstractions;

public interface IPublicCourseCatalogSearchService
{
    // S107: the parameters mirror the catalog's query string one-to-one. A filter object would add a type
    // whose only job is to be unpacked again, and it would still have to stay in step with the query.
#pragma warning disable S107
    Task<PaginatedResult<PublicCourseCardDto>> SearchAsync(
        string? search,
        PaginationRequest pagination,
        Guid? categoryId,
        Guid? instructorId,
        string? sortBy,
        bool? isFree,
        decimal? minRating,
        CancellationToken cancellationToken);
#pragma warning restore S107
}
