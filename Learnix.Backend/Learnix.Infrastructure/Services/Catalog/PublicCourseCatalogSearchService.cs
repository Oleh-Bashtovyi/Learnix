using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Queries.GetPublicCourses;
using Learnix.Domain.Enums;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Services.Catalog;

internal sealed class PublicCourseCatalogSearchService(
    ApplicationDbContext context,
    IBlobStorageService blobStorage)
    : IPublicCourseCatalogSearchService
{
#pragma warning disable S107 // Mirrors IPublicCourseCatalogSearchService.SearchAsync.
    public async Task<PaginatedResult<PublicCourseCardDto>> SearchAsync(
        string? search,
        PaginationRequest pagination,
        Guid? categoryId,
        Guid? instructorId,
        string? sortBy,
        bool? isFree,
        decimal? minRating,
        CancellationToken cancellationToken)
#pragma warning restore S107
    {
        var query = ApplyFilters(
            context.Courses.AsNoTracking().Where(c => c.Status == CourseStatus.Published),
            search,
            categoryId,
            instructorId,
            isFree,
            minRating);

        var orderedQuery = ApplySort(query, sortBy, search);

        var totalCount = await orderedQuery.LongCountAsync(cancellationToken);

        if (totalCount == 0)
            return PaginatedResult<PublicCourseCardDto>.Empty(pagination.PageIndex, pagination.PageSize);

        var courses = await orderedQuery
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .Join(context.Categories, c => c.CategoryId, cat => cat.Id,
                (c, cat) => new { c, CategoryName = cat.Name })
            .Join(context.Users, x => x.c.InstructorId, u => u.Id,
                (x, u) => new
                {
                    x.c.Id,
                    x.c.InstructorId,
                    x.c.CategoryId,
                    x.c.Title,
                    x.c.Description,
                    x.c.CoverBlobPath,
                    x.c.Price,
                    x.c.EnrollmentsCount,
                    x.c.AverageRating,
                    x.c.ReviewsCount,
                    x.c.Tags,
                    x.CategoryName,
                    u.FirstName,
                    u.LastName,
                })
            .ToListAsync(cancellationToken);

        var cards = courses
            .Select(c => new PublicCourseCardDto(
                c.Id,
                c.InstructorId,
                c.CategoryId,
                c.Title,
                c.Description,
                !string.IsNullOrWhiteSpace(c.CoverBlobPath) ? blobStorage.GetPublicUrl(c.CoverBlobPath) : null,
                c.Price,
                c.Price == 0m,
                c.EnrollmentsCount,
                c.Tags.ToList(),
                c.AverageRating,
                c.ReviewsCount,
                c.CategoryName,
                $"{c.FirstName} {c.LastName}"))
            .ToList();

        return PaginatedResult<PublicCourseCardDto>.Create(
            cards,
            pagination.PageIndex,
            pagination.PageSize,
            totalCount);
    }

    private static IQueryable<Domain.Entities.Course> ApplyFilters(
        IQueryable<Domain.Entities.Course> query,
        string? search,
        Guid? categoryId,
        Guid? instructorId,
        bool? isFree,
        decimal? minRating)
    {
        if (categoryId.HasValue)
            query = query.Where(c => c.CategoryId == categoryId.Value);

        if (instructorId.HasValue)
            query = query.Where(c => c.InstructorId == instructorId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => EF.Functions.ILike(c.Title, $"%{search.Trim()}%"));

        if (isFree.HasValue)
            query = isFree.Value ? query.Where(c => c.Price == 0m) : query.Where(c => c.Price > 0m);

        if (minRating.HasValue)
            query = query.Where(c => c.AverageRating >= minRating.Value);

        return query;
    }

    /// <summary>
    /// Without an explicit sort, a search falls back to relevance (exact title, then prefix, then the rest)
    /// and a plain listing falls back to popularity.
    /// </summary>
    private static IQueryable<Domain.Entities.Course> ApplySort(
        IQueryable<Domain.Entities.Course> query,
        string? sortBy,
        string? search)
    {
        var sort = sortBy?.ToLower();

        if (sort == "newest")
            return query.OrderByDescending(c => c.CreatedAt);

        if (sort == "rating")
        {
            return query
                .OrderByDescending(c => c.AverageRating)
                .ThenByDescending(c => c.ReviewsCount);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowered = search.Trim().ToLower();

            return query
                .OrderBy(c => c.Title.ToLower() == lowered ? 0 : c.Title.ToLower().StartsWith(lowered) ? 1 : 2)
                .ThenByDescending(c => c.EnrollmentsCount)
                .ThenByDescending(c => c.UpdatedAt);
        }

        return query
            .OrderByDescending(c => c.EnrollmentsCount)
            .ThenByDescending(c => c.UpdatedAt);
    }
}
