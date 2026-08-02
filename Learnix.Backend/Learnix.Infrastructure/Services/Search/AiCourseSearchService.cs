using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.SearchCourses;
using Learnix.Domain.Enums;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Services.Search;

internal sealed class AiCourseSearchService(ApplicationDbContext context) : IAiCourseSearchService
{
    public async Task<IReadOnlyList<CourseSearchResultDto>> SearchAsync(
        string query,
        Guid? categoryId,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var matched = context.Courses.AsNoTracking()
            .Where(c => c.Status == CourseStatus.Published)
            .WhereMatchesSearch(query);

        if (categoryId.HasValue)
            matched = matched.Where(c => c.CategoryId == categoryId.Value);

        var rows = await matched
            .OrderByRelevance(query)
            .Take(maxResults)
            .Join(context.Categories, c => c.CategoryId, cat => cat.Id,
                (c, cat) => new { c, CategoryName = cat.Name })
            .Join(context.Users, x => x.c.InstructorId, u => u.Id,
                (x, u) => new
                {
                    x.c.Id,
                    x.c.Title,
                    x.c.Description,
                    x.CategoryName,
                    x.c.InstructorId,
                    InstructorFullName = u.FirstName + " " + u.LastName,
                    x.c.Price,
                    x.c.EnrollmentsCount,
                })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new CourseSearchResultDto(
                r.Id,
                r.Title,
                r.Description.Length > AiChatToolLimits.CourseDescriptionPreviewLength
                    ? r.Description[..AiChatToolLimits.CourseDescriptionPreviewLength] + "..."
                    : r.Description,
                r.CategoryName,
                r.InstructorId,
                r.InstructorFullName,
                r.Price,
                r.Price == 0,
                r.EnrollmentsCount))
            .ToList();
    }
}
