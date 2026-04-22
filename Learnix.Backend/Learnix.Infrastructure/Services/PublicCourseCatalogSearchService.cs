using Learnix.Application.Common.Pagination;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Queries.GetPublicCourses;
using Learnix.Domain.Enums;
using Learnix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Services;

internal sealed class PublicCourseCatalogSearchService(ApplicationDbContext context)
    : IPublicCourseCatalogSearchService
{
    public async Task<PaginatedResult<PublicCourseCardDto>> SearchAsync(
        string? search,
        PaginationRequest pagination,
        Guid? categoryId,
        CancellationToken ct)
    {
        var query = context.Courses
            .AsNoTracking()
            .Where(c => c.Status == CourseStatus.Published);

        if (categoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            var lowered = normalized.ToLower();

            query = query
                .Where(c => EF.Functions.ILike(c.Title, $"%{normalized}%"))
                .OrderBy(c => c.Title.ToLower() == lowered ? 0 : c.Title.ToLower().StartsWith(lowered) ? 1 : 2)
                .ThenByDescending(c => c.EnrollmentsCount)
                .ThenByDescending(c => c.UpdatedAt);
        }
        else
        {
            query = query
                .OrderByDescending(c => c.EnrollmentsCount)
                .ThenByDescending(c => c.UpdatedAt);
        }

        var totalCount = await query.LongCountAsync(ct);

        if (totalCount == 0)
            return PaginatedResult<PublicCourseCardDto>.Empty(pagination.PageIndex, pagination.PageSize);

        var courses = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .Select(c => new PublicCourseCardDto(
                c.Id,
                c.InstructorId,
                c.CategoryId,
                c.Title,
                c.Description,
                c.CoverImageUrl,
                c.Price,
                c.Price == 0m,
                c.EnrollmentsCount,
                c.Tags.ToList()))
            .ToListAsync(ct);

        return PaginatedResult<PublicCourseCardDto>.Create(
            courses,
            pagination.PageIndex,
            pagination.PageSize,
            totalCount);
    }
}
