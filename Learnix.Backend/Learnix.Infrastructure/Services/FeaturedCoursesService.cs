using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Queries.GetFeaturedCourses;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Services;

internal sealed class FeaturedCoursesService(
    ApplicationDbContext context,
    IBlobStorageService blobStorage)
    : IFeaturedCoursesService
{
    private const int BestsellerSlots = 2;
    private static readonly TimeSpan NewCourseCutoff = TimeSpan.FromDays(90);

    public async Task<IReadOnlyList<FeaturedCourseDto>> GetTopFeaturedAsync(int count, CancellationToken ct)
    {
        var raw = await (
            from c in context.Courses
            join cat in context.Categories on c.CategoryId equals cat.Id
            join u in context.Users on c.InstructorId equals u.Id
            where c.Status == CourseStatus.Published
            orderby c.EnrollmentsCount descending, c.AverageRating descending
            select new
            {
                c.Id,
                c.Title,
                c.Description,
                c.CoverBlobPath,
                c.Price,
                c.AverageRating,
                c.ReviewsCount,
                c.CreatedAt,
                CategoryName = cat.Name,
                InstructorId = u.Id,
                InstructorFirstName = u.FirstName,
                InstructorLastName = u.LastName,
            }
        ).Take(count).ToListAsync(ct);

        if (raw.Count == 0)
            return [];

        var courseIds = raw.Select(r => r.Id).ToList();

        var durationByCourse = await context.Sections
            .Where(s => courseIds.Contains(s.CourseId))
            .Join(
                context.Lessons.OfType<VideoLesson>(),
                s => s.Id,
                l => l.SectionId,
                (s, l) => new { s.CourseId, DurationSeconds = l.DurationSeconds ?? 0 })
            .GroupBy(x => x.CourseId)
            .Select(g => new { CourseId = g.Key, TotalSeconds = g.Sum(x => x.DurationSeconds) })
            .ToDictionaryAsync(x => x.CourseId, x => x.TotalSeconds, ct);

        var cutoff = DateTime.UtcNow - NewCourseCutoff;

        return raw.Select((item, idx) =>
        {
            var badge = idx < BestsellerSlots ? "bestseller"
                : item.CreatedAt >= cutoff ? "new"
                : (string?)null;

            var totalSeconds = durationByCourse.TryGetValue(item.Id, out var secs) ? secs : 0;
            var durationHours = Math.Round(totalSeconds / 3600.0, 1);

            return new FeaturedCourseDto(
                item.Id,
                item.Title,
                item.Description,
                item.CoverBlobPath is not null
                    ? blobStorage.GenerateReadUrl(item.CoverBlobPath, TimeSpan.FromHours(24))
                    : null,
                item.Price,
                item.Price == 0m,
                item.AverageRating,
                item.ReviewsCount,
                durationHours,
                item.CategoryName,
                new FeaturedCourseInstructorDto(
                    item.InstructorId,
                    $"{item.InstructorFirstName} {item.InstructorLastName}"),
                badge);
        }).ToList();
    }
}
