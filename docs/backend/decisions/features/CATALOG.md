# Learnix — ADR: Course Catalog & Search

> Covers public course discovery — the catalog page's filters/search and the AI assistant's
> `search_courses` tool, which share the same underlying search.

---

## ADR-BACK-CATALOG-001: PostgreSQL Full-Text Search, Shared Between the Catalog and the AI Assistant

**Context:** substring matching (`Contains(keyword)`) has no stemming, no stopwords and no ranking — a
search for "testing" missed "tests", and results were ordered by enrollment count regardless of relevance.

**Decision:** Course search — both the public catalog (`GET /courses?search=`) and the AI tool
(`search_courses`) — runs on a real PostgreSQL full-text search instead of a substring match, and
both consumers compose the same match-and-rank primitive rather than maintaining two
implementations.

**Where it lives:**

- A generated, `STORED` `tsvector` column on `Courses` (`Course.SearchVector`, a shadow property —
  `Course` itself gains no new member), configured in `CourseConfiguration`:
  ```sql
  setweight(to_tsvector('english'::regconfig, coalesce("Title", '')), 'A') ||
  setweight(to_tsvector('english'::regconfig, coalesce("Description", '')), 'B') ||
  setweight(array_to_tsvector(coalesce("Tags", ARRAY[]::text[])), 'C')
  ```
  Title outranks Description outranks Tags. A GIN index (`IX_Courses_SearchVector`) backs it.
  Migration: `AddCourseFullTextSearch`.
- One shared match-and-rank primitive, `CourseFullTextSearchExtensions`
  (`Learnix.Infrastructure/Services/Search/`) — two `IQueryable<Course>` extension methods,
  `WhereMatchesSearch(text)` and `OrderByRelevance(text)`, both built on
  `EF.Functions.WebSearchToTsQuery` + `RankCoverDensity`. Every consumer composes these two methods
  rather than writing its own predicate.
- The **public catalog** (`PublicCourseCatalogSearchService`, Infrastructure) calls them directly —
  `ApplyFilters` for matching, `ApplySort`'s relevance branch for ranking — alongside its own
  pagination, `isFree`/`minRating` filters and sort modes (`newest`/`rating`/`relevance`/`popularity`),
  none of which the AI tool needs.
- The **AI tool** goes through a seam, `IAiCourseSearchService` (defined in
  `Learnix.Application/AiChat/Abstractions/`, implemented by `AiCourseSearchService` in
  `Learnix.Infrastructure/Services/Search/`, registered in `CatalogModule`) — mirroring the seam
  `IPublicCourseCatalogSearchService` already established for the catalog. `SearchCoursesQueryHandler`
  calls it with the query text, an optional category id (resolved from the tool's category *slug* via
  `CategoryBySlugSpecification`) and a clamped `maxResults`; the service matches, ranks, and joins
  `Categories`/`Users` inline in one query to build `CourseSearchResultDto` — one round trip, not
  three.

**Why a shared primitive and not a shared HTTP endpoint or a shared MediatR query:** the two
callers' filters, limits and output DTOs genuinely differ — the catalog paginates and offers four
sort modes over a rich card DTO; the AI tool takes a flat `maxResults`, no pagination, and returns a
compact DTO shaped for token cost. Forcing them onto one endpoint or one query/handler would mean
one of the two carrying parameters or fields it never uses. What both needed unified was the *search
itself* — the `Where`/`OrderBy` predicate against Postgres — and that is exactly the seam
`CourseFullTextSearchExtensions` draws.

**Why this replaces per-keyword substring matching:** the previous implementation
(`CourseSearchSpecification`, deleted) ANDed a `Title/Description/Tags.Contains(keyword)` predicate
per keyword — no stemming ("testing" did not find "tests"), no stopwords (a filler word in the query
narrowed the result set to nothing), no ranking (results were ordered by enrollment count regardless
of relevance), and no usable index (`LOWER(col) LIKE '%x%'` cannot use a B-tree). A keyword that
matched nothing sank the whole query, so `SearchCoursesQueryHandler` retried with each keyword taken
separately and unioned the results — up to one database round trip per keyword, with no cap on how
many keywords a query could contain. `websearch_to_tsquery` parses the query text natively —
multi-word phrases, `or`, `-exclusions`, stopword removal — in a single query, which is what let both
loops (the per-keyword `AND` and the fallback) be deleted rather than merely bounded.

**Why `Application` still does not reference `Microsoft.EntityFrameworkCore`:** every tsvector/rank
call (`EF.Functions.*`, `NpgsqlTsVector`) lives in `Learnix.Infrastructure`, behind
`IAiCourseSearchService` for the AI tool and inside `PublicCourseCatalogSearchService` for the
catalog. Neither the AI tool's handler nor any Application-layer type ever sees an EF Core or Npgsql
type.

**Two PostgreSQL immutability gotchas, worth recording because they are easy to reintroduce:**

- `to_tsvector('english', text)` is only usable in a generated column when the config argument is
  cast to `regconfig` (`to_tsvector('english'::regconfig, …)`) — the plain-text-literal overload is
  otherwise resolved to a `STABLE` function signature, and Postgres refuses `STABLE` expressions in a
  generated column (`generation expression is not immutable`).
- Flattening the `Tags` array into a string for `to_tsvector` is **not** immutable by any of the
  obvious routes: `array_to_string(text[], text)` and the `anyarray::text` cast are both `STABLE` in
  Postgres (verified against `pg_proc.provolatile` on `postgres:16-alpine`), so neither works in a
  generated column. `array_to_tsvector(text[])` is `IMMUTABLE` and used instead — it treats each array
  element as one lexeme directly, skipping `to_tsvector`'s text parser entirely.
  - **Trade-off:** skipping the parser also skips its lowercasing/stemming, so a tag is matched as an
    exact-case lexeme — a tag stored as `Python` only matches a search typed `Python`, not `python`.
    Tags carry the lowest weight (C); Title and Description, the dominant signal, go through
    `to_tsvector` and are fully normalized. Left as a known, minor limitation rather than fixed by
    lowercasing tags at write time, which would be a display-visible behavior change (tag chips render
    whatever casing is stored) outside this decision's scope.

**Why not a custom immutable SQL wrapper function for the Tags join instead:** a hand-written
`IMMUTABLE` SQL function (e.g. wrapping `array_to_string`) would need to exist *before* the migration
that references it in a generated column runs. Functions/triggers/views in this codebase are
deliberately repeatable scripts under `DatabaseObjects/*.sql`, applied *after* `MigrateAsync()`
completes (ADR-BACK-MIGR-003 in `platform/MIGRATIONS.md`) — exactly the wrong order for a migration
that needs the function to already exist. Reordering the applier to run before migrations would fix
this one case but invert the dependency direction every existing repeatable object relies on (a
trigger needs its table to already exist). `array_to_tsvector` avoids the conflict entirely by needing
no custom function.

**Testing consequence:** `Learnix.Infrastructure.UnitTests` exercises `ApplicationDbContext` (real
entity configurations, including `Course.SearchVector`) against the EF Core **InMemory** provider to
test interceptors in isolation — see `operations/TESTING.md` for why interceptor tests don't need real
Postgres. InMemory cannot map `NpgsqlTsVector`, so `ApplicationDbContext.OnModelCreating` calls
`builder.Entity<Course>().Ignore("SearchVector")` whenever `Database.IsNpgsql()` is false. The shadow
property is real in every environment that talks to actual Postgres (dev, CI/CD, `IntegrationTests`,
which boots the full `ApplicationDbContext` against a Testcontainers Postgres).

**Consequences:**
- `search_courses` results are now cached (`CacheKeys.AiChat.CourseSearch`, 15-minute TTL — looser
  than the catalog's 5-minute `Courses.PublicTtl`, since a conversational recommendation does not need
  to reflect a just-published course within minutes). Not explicitly invalidated, the same accepted
  trade-off already on record for `Courses.Public`.
- The AI tool's `query` argument is bounded by `SearchCoursesQueryValidator`
  (`CourseValidationConstants.SearchMaxLength`, the catalog's own 100-character bound) rather than a
  second, AI-specific ceiling — both run the same search now, so one bound suffices.
- `CourseSearchSpecification` and the AiChat-local `UsersByIdsSpecification` (only ever used to batch-
  resolve instructor names for that spec's results) are deleted.

**Rejected alternatives:**
- Keeping two independent substring-matching implementations (the state before this decision) —
  divergent behavior for the same conceptual operation, and the AI tool's fallback loop scaled with
  the number of keywords in the query with no cap.
- `pg_trgm` trigram indexes over `LOWER(Title)`/`LOWER(Description)` with `ILIKE` — appeared in an
  earlier draft of this documentation (`ADR-BACK-CHAT-006`) but the migration for it was never
  actually written; substring matching under any indexing strategy still lacks stemming, stopwords and
  relevance ranking, which is what motivated moving to full-text search in the first place.
- A shared MediatR query or a shared HTTP endpoint for both callers — rejected above; the callers'
  filters and DTOs are genuinely different, only the match/rank predicate is shared.
- A custom `IMMUTABLE` SQL function for joining `Tags` into a searchable string — rejected above over
  the migration/repeatable-script ordering conflict.
- Lowercasing `Tags` at write time to fix the Tags case-sensitivity trade-off — would change what tag
  chips already display, a separate, visible product decision outside this pass.
