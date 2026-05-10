using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Queries.GetPublicCourses;
using Learnix.Domain.Enums;
using Learnix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Services;

internal sealed class PublicCourseCatalogSearchService(
    ApplicationDbContext context,
    IBlobStorageService blobStorage)
    : IPublicCourseCatalogSearchService
{
    public async Task<PaginatedResult<PublicCourseCardDto>> SearchAsync(
        string? search,
        PaginationRequest pagination,
        Guid? categoryId,
        Guid? instructorId,
        string? sortBy,
        bool? isFree,
        decimal? minRating,
        CancellationToken ct)
    {
        IQueryable<Domain.Entities.Course> query = context.Courses
            .AsNoTracking()
            .Where(c => c.Status == CourseStatus.Published);

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

        // Apply sort
        var sort = sortBy?.ToLower();
        IQueryable<Domain.Entities.Course> orderedQuery;

        if (sort == "newest")
        {
            orderedQuery = query.OrderByDescending(c => c.CreatedAt);
        }
        else if (sort == "rating")
        {
            orderedQuery = query
                .OrderByDescending(c => c.AverageRating)
                .ThenByDescending(c => c.ReviewsCount);
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            // Relevance-first when searching without explicit sort
            var lowered = search.Trim().ToLower();
            orderedQuery = query
                .OrderBy(c => c.Title.ToLower() == lowered ? 0 : c.Title.ToLower().StartsWith(lowered) ? 1 : 2)
                .ThenByDescending(c => c.EnrollmentsCount)
                .ThenByDescending(c => c.UpdatedAt);
        }
        else
        {
            orderedQuery = query
                .OrderByDescending(c => c.EnrollmentsCount)
                .ThenByDescending(c => c.UpdatedAt);
        }

        var totalCount = await orderedQuery.LongCountAsync(ct);

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
            .ToListAsync(ct);

        var cards = courses
            .Select(c => new PublicCourseCardDto(
                c.Id,
                c.InstructorId,
                c.CategoryId,
                c.Title,
                c.Description,
                c.CoverBlobPath is not null
                    ? blobStorage.GenerateReadUrl(c.CoverBlobPath, TimeSpan.FromHours(24))
                    : null,
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
}
