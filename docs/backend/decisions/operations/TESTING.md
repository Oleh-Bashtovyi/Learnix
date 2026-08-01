# Learnix — ADR: Testing

> How the backend is tested, and where the line between unit and integration sits.

---

## ADR-BACK-TEST-001: Integration tests run the real app on real Postgres and Redis, not the EF in-memory provider

**Context:** a test running against EF Core's in-memory provider has no real constraints, foreign keys or
SQL, so it can pass on a migration or query that would break production, or fail on one that works fine.

**Decision:** Integration tests live in `Learnix.IntegrationTests` and boot the actual API through
`WebApplicationFactory<Program>`, against **real Postgres and Redis** in throwaway Docker containers
(Testcontainers). A request under test travels the pipeline it travels in production — routing,
`[Authorize]`, the MediatR validation/caching behaviors, EF against Postgres, the Redis cache — and only
two things are substituted:

- **Blob storage**, by an in-memory stub. No container CRUD touches a file; the SAS/commit flow is not
  what these tests are about.
- **The hosted background services**, removed. The outbox processor holds a `LISTEN/NOTIFY` connection
  and SignalR spins up a hub; neither is exercised here, and both would run concurrently with the
  per-test database reset and make results non-deterministic.

Each test starts from a clean slate: `Respawner` truncates the Postgres tables and the fixture issues a
Redis `FLUSHDB`. The containers start **once** for the whole run via an xUnit `ICollectionFixture`, and
the collection runs serially so the shared reset is safe.

**Why not `Microsoft.EntityFrameworkCore.InMemory`:** it is not a database. It has no unique constraints,
no foreign keys, no real SQL, no transactions — so a `409` from a duplicate slug, a cascade, or a
provider-specific query either passes when production would fail or fails when production would pass. A
test that green-lights a broken migration is worse than no test. Testcontainers gives the real engine at
the cost of a Docker daemon and ~15 s of startup, which is the trade the .NET ecosystem has settled on.

**Why the token is real, not a faked identity:** `ClientWithRoles(...)` mints an actual signed JWT via
the app's own `ITokenService` and sends it as a bearer token, so the authentication and authorization
middleware run for real. A `TestAuthHandler` that injects a `ClaimsPrincipal` would skip exactly the
layer these tests exist to cover — the route attribute that ADR-BACK-AUTH-018 made the *only* role gate.

**What this suite is the first to cover:**
- That `[Authorize(Roles = ...)]` on the route actually stops a student (`403` + `insufficient_role`)
  and an anonymous caller (`401`). When the duplicate handler-level role checks were deleted
  (ADR-BACK-AUTH-018), their unit tests went too; nothing else verifies the surviving gate, because it
  is now HTTP middleware, not handler code.
- That the `403` carries the machine-readable `code` from `ProblemDetailsAuthorizationResultHandler`.
- Cache invalidation: creating a category evicts `CacheKeys.Categories.All`, observable only across two
  requests and therefore invisible to any unit test.

**Scope — extended by what proves risky, not covered wholesale:** it started with category CRUD alone
and has since grown to course-structure mutations (sections and lessons across all three lesson types —
create, update, delete, reorder, visibility), catalog search, and wishlist. Integration tests are the
expensive tier; they earn their keep on the paths where the interesting behavior lives *between* the
components — auth enforcement, real constraints, cache coherence, ordering invariants — not on every
handler, which the unit tests already cover in isolation. A feature graduates into this suite when its
risk is in the wiring, not the logic — that is still the bar for the next addition, not a list to keep
in sync here.

**Consequences:**
- `dotnet test Learnix.Backend.slnx` now **requires a running Docker daemon**. Without one, the
  integration project fails to start its containers; the three unit projects are unaffected. CI on
  `ubuntu-latest` has Docker, so nothing in the workflow changes.
- `Program.cs` ends with `public partial class Program;` — the standard way to make a top-level-statements
  entry point visible to `WebApplicationFactory<Program>`.
- The reset clears **both** stores. An earlier version truncated only Postgres, and the cache-invalidation
  test caught it: a primed Redis entry survived into the next test. The failing test is the reason the
  reset now flushes Redis too.
