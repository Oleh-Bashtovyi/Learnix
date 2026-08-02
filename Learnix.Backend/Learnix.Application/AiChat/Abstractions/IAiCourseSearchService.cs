using Learnix.Application.AiChat.Queries.SearchCourses;

namespace Learnix.Application.AiChat.Abstractions;

/// <summary>
/// The AI tool's course search, backed by the same PostgreSQL full-text search the public catalog
/// uses (<see cref="Courses.Abstractions.IPublicCourseCatalogSearchService"/>) — see
/// ADR-BACK-CATALOG-001 in docs/backend/decisions/features/CATALOG.md. A separate seam because the
/// AI tool's filters, limits and output DTO genuinely differ from the catalog's (no pagination, no
/// isFree/minRating, a compact token-frugal DTO), not because the underlying match+rank logic is
/// duplicated.
/// </summary>
public interface IAiCourseSearchService
{
    Task<IReadOnlyList<CourseSearchResultDto>> SearchAsync(
        string query,
        Guid? categoryId,
        int maxResults,
        CancellationToken cancellationToken);
}
