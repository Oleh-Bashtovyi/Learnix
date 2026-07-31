using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Learnix.Infrastructure.Services.Search;

/// <summary>
/// The one place that builds a full-text match or rank against <c>Course.SearchVector</c> (the
/// generated, weighted tsvector configured in <c>CourseConfiguration</c> — Title/A, Description/B,
/// Tags/C). Both the public catalog (<see cref="Catalog.PublicCourseCatalogSearchService"/>) and the
/// AI tool (<see cref="AiCourseSearchService"/>) compose these two extensions rather than each
/// building their own predicate — see ADR-BACK-CATALOG-001 in
/// docs/backend/decisions/features/CATALOG.md.
/// </summary>
internal static class CourseFullTextSearchExtensions
{
    // Course titles and descriptions are English by policy; the system prompt tells the AI to
    // translate keywords before searching, so a single fixed config is correct today.
    private const string TsConfig = "english";
    private const string SearchVectorProperty = "SearchVector";

    // EF.Functions.* stubs throw at runtime unless the call appears literally inside a query
    // expression tree, where EF's LINQ visitor pattern-matches it and translates it to SQL. Computing
    // the NpgsqlTsQuery as a local variable outside the Where/OrderByDescending lambda — the previous
    // shape of these two methods — executes it as plain C#, hits the throwing stub immediately, and
    // never reaches Postgres at all. Both calls below stay inside the lambda for exactly this reason.
    internal static IQueryable<Course> WhereMatchesSearch(this IQueryable<Course> query, string searchText) =>
        query.Where(c => EF.Property<NpgsqlTsVector>(c, SearchVectorProperty)
            .Matches(EF.Functions.WebSearchToTsQuery(TsConfig, searchText)));

    internal static IOrderedQueryable<Course> OrderByRelevance(this IQueryable<Course> query, string searchText) =>
        query.OrderByDescending(c => EF.Property<NpgsqlTsVector>(c, SearchVectorProperty)
            .RankCoverDensity(EF.Functions.WebSearchToTsQuery(TsConfig, searchText)));
}
