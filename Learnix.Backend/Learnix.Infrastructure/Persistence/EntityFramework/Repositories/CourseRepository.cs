using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class CourseRepository(ApplicationDbContext context)
    : RepositoryBase<Course>(context), ICourseRepository
{
    public async Task<IReadOnlyList<string>> GetPopularTagsAsync(
        Guid? categoryId,
        int limit,
        int minCourses,
        CancellationToken cancellationToken = default)
    {
        // SQL is required because Npgsql maps Tags as a native text[] with no SelectMany
        // translation on EF 8. Grouping by tag has to be expressed here to run in Postgres.
        // The global soft-delete filter does not reach raw SQL, which is why IsDeleted is excluded here.
        // Also, a null Guid parameter has no type Postgres can infer, which is why the optional category filter
        // is driven by a bool parameter and a Guid that is never null.
        //
        // Published only: a draft's tags are one author's working notes, so counting them would
        // suggest vocabulary no student has seen and let an instructor seed the list with drafts.
        var hasCategory = categoryId.HasValue;
        var category = categoryId ?? Guid.Empty;

        FormattableString sql = $"""
            SELECT tag AS "Value"
            FROM "Courses" c, unnest(c."Tags") AS tag
            WHERE c."Status" = {(int)CourseStatus.Published}
              AND c."IsDeleted" = FALSE
              AND ({hasCategory} = FALSE OR c."CategoryId" = {category})
            GROUP BY tag
            HAVING COUNT(*) >= {minCourses}
            ORDER BY COUNT(*) DESC, tag
            LIMIT {limit}
            """;

        return await context.Database.SqlQuery<string>(sql).ToListAsync(cancellationToken);
    }

    public async Task<(string CategoryName, string InstructorFullName)> GetCategoryAndInstructorNamesAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var names = await (
            from c in context.Courses
            join cat in context.Categories on c.CategoryId equals cat.Id
            join u in context.Users on c.InstructorId equals u.Id
            where c.Id == courseId
            select new { cat.Name, InstructorFullName = u.FirstName + " " + u.LastName }
        ).FirstOrDefaultAsync(cancellationToken);

        return names is null ? (string.Empty, string.Empty) : (names.Name, names.InstructorFullName);
    }
}
