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
/// - ADR-BACK-ARCH-013: CacheKeys in Application layer, not Domain
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

        public static readonly TimeSpan PublicTtl = TimeSpan.FromMinutes(5);
    }

    public static class AiChat
    {
        /// <summary>
        /// The AI provider outage in force, if any (ADR-CHAT-014). No TTL constant: the entry expires when
        /// the provider said it would be worth calling again, so the key's own lifetime ends the outage.
        /// </summary>
        public static string Outage => "ai-chat:outage";
    }
}
