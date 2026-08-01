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
        // SQL, because Npgsql maps Tags as a native text[] with no SelectMany translation on EF 8
        // (see CourseConfiguration): grouping by tag has to be expressed here to run in Postgres.
        //
        // Two rules this SQL carries that the LINQ pipeline would apply on its own:
        //  - the global soft-delete filter does not reach raw SQL, so IsDeleted is excluded here;
        //  - a null Guid parameter has no type Postgres can infer, so the optional category filter
        //    is driven by a bool parameter and a Guid that is never null.
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
}
