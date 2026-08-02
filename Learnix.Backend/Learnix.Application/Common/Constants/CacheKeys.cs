namespace Learnix.Application.Common.Constants;

/// <summary>
/// Central registry of distributed-cache keys and their time-to-live values.
/// </summary>
/// <remarks>
/// Each key is co-located with its TTL so the two cannot drift apart: a query that
/// caches under <c>Courses.Featured</c> must use <c>Courses.FeaturedTtl</c>.
///
/// TTLs live here rather than inline in the query records, so that every cache-lifetime
/// decision is visible in one file alongside the invalidation story for that key.
///
/// Related ADRs:
/// - ADR-BACK-INFRA-002: Redis distributed cache
/// - ADR-BACK-INFRA-016: CacheKeys in Application layer, not Domain
/// - ADR-BACK-INFRA-017: cache keys and their TTLs are co-located here
/// </remarks>
public static class CacheKeys
{
    public static class Categories
    {
        /// <summary>Full category list. Explicitly invalidated by every category mutation.</summary>
        public static string All => "categories:all";

        /// <summary>
        /// A long TTL is safe here: every create/update/delete/image command removes this key,
        /// so expiry is only a backstop, never the primary freshness mechanism.
        /// </summary>
        public static readonly TimeSpan AllTtl = TimeSpan.FromHours(24);
    }

    public static class Courses
    {
        /// <summary>Course detail. Invalidated on course mutations and on review create/update/delete.</summary>
        public static string ById(Guid id) => $"course:{id}";

        public static readonly TimeSpan ByIdTtl = TimeSpan.FromMinutes(10);

        /// <summary>Featured courses on the landing page. Invalidated on any course lifecycle change.</summary>
        public static string Featured => "courses:featured";

        public static readonly TimeSpan FeaturedTtl = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Public catalog page. The key embeds every filter parameter, so each unique
        /// filter combination produces its own entry.
        /// </summary>
        /// <remarks>
        /// Callers MUST pass values already normalized the same way the query normalizes them
        /// before hitting the database, otherwise requests that return identical rows are cached
        /// under different keys:
        /// <list type="bullet">
        ///   <item><paramref name="search"/> - trimmed and lower-cased, or null when blank.
        ///     Safe to fold case because the filter uses <c>ILike</c> and the relevance sort compares
        ///     <c>lower(title)</c> on both sides, so "React" and "react" select and order identically.</item>
        ///   <item><paramref name="pageIndex"/>/<paramref name="pageSize"/> - the values produced by
        ///     <c>PaginationRequest.FromOffset(skip, take)</c>, not the raw skip/take from the query
        ///     string. Raw skip 0..19 with take 20 all resolve to page 0.</item>
        /// </list>
        ///
        /// NOT explicitly invalidated: <c>IDistributedCache</c> exposes no prefix or tag deletion.
        /// Freshness relies solely on <see cref="PublicTtl"/>, so the catalog can lag a course
        /// publish by up to that duration. Accepted trade-off. The key space is bounded only by
        /// the search-term length cap (<c>CourseValidationConstants.SearchMaxLength</c>).
        /// </remarks>
        // S107: every parameter is part of the cache identity. Collapsing them into an object would
        // just move the same eight values behind a type and make drift from the query easier.
#pragma warning disable S107
        public static string Public(
            string? search,
            int pageIndex,
            int pageSize,
            Guid? categoryId,
            Guid? instructorId,
            string? sortBy,
            bool? isFree,
            decimal? minRating)
            => $"courses:public:{search}:{pageIndex}:{pageSize}:{categoryId}:{instructorId}:{sortBy}:{isFree}:{minRating}";
#pragma warning restore S107

        public static readonly TimeSpan PublicTtl = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Total published-course count, surfaced to the AI assistant (<c>get_platform_stats</c>).
        /// Explicitly invalidated by every publish/unpublish/archive/delete/recover command handler,
        /// right alongside their existing <see cref="Featured"/> invalidation. The TTL below is a
        /// backstop only, the same trade-off <see cref="Categories.All"/> documents.
        /// </summary>
        public static string PublishedCount => "courses:published-count";

        public static readonly TimeSpan PublishedCountTtl = TimeSpan.FromHours(24);

        /// <summary>
        /// The tags most published courses of a category carry. A null category is the
        /// platform-wide list and holds its own entry. The key space is bounded by the number of
        /// categories: the result limit and the popularity threshold are constants
        /// (<c>CourseTagConstants</c>), not query parameters, so a caller cannot mint entries.
        ///
        /// NOT explicitly invalidated: freshness relies on <see cref="PopularTagsTtl"/>, so the list
        /// can lag a course publish by up to that duration.
        /// </summary>
        public static string PopularTags(Guid? categoryId) => $"courses:popular-tags:{categoryId}";

        public static readonly TimeSpan PopularTagsTtl = TimeSpan.FromHours(1);
    }

    public static class AiChat
    {
        /// <summary>
        /// The AI provider outage in force, if any (ADR-BACK-CHAT-014). No TTL constant: the entry expires when
        /// the provider said it would be worth calling again, so the key's own lifetime ends the outage.
        /// </summary>
        public static string Outage => "ai-chat:outage";

        /// <summary>
        /// The <c>search_courses</c> tool result. Keyed on the normalized query text — full-text search
        /// (ADR-BACK-CATALOG-001) is deterministic for identical input, so the trimmed/lower-cased
        /// string plus category and the clamped result count are enough to identify a repeated search.
        /// </summary>
        public static string CourseSearch(string query, string? category, int maxResults)
            => $"ai-chat:course-search:{query}:{category}:{maxResults}";

        /// <summary>
        /// Looser than the live catalog's 5-minute TTL (<see cref="Courses.PublicTtl"/>): this backs a
        /// conversational recommendation, not a page a student expects to reflect a just-published course
        /// within minutes. Not explicitly invalidated, same accepted trade-off as <see cref="Courses.Public"/> —
        /// the key space is bounded by <c>SearchCoursesQueryValidator</c>'s length cap on the query.
        /// </summary>
        public static readonly TimeSpan CourseSearchTtl = TimeSpan.FromMinutes(15);
    }
}
