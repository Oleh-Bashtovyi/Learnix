# Learnix — ADR: Migrations & Seeding

> Format: what was decided → why → what alternatives were rejected.
> Updated after each chat where migration/seeding decisions were made.

Related files: [INFRA.md](INFRA.md) · [OUTBOX.md](OUTBOX.md) · [CICD.md](../operations/CICD.md) · [BLOB.md](BLOB.md)

## Status Convention

ADRs are not deleted. If a decision is reviewed — the old ADR is marked `Superseded by ADR-XXX`, the new one — `Supersedes ADR-YYY`. This preserves the history of thought and shows how the architecture evolved.

---

## ADR-BACK-MIGR-001: Migrations and seeding live in a standalone `Learnix.DbMigrator`

**Context:** migrations and seeding ran inside the API and its Infrastructure project, which meant every
API replica could try to apply them at once on startup, and the API carried DDL rights it has no runtime
use for.

**Decision:** all schema migration and data seeding execution was extracted from `Learnix.API` and `Learnix.Infrastructure` into a standalone console application, `Learnix.DbMigrator`.

1. **The API never applies migrations — in any environment.** `Learnix.DbMigrator` calls `Database.MigrateAsync()`, and it is the only thing in the solution that does; `Learnix.Infrastructure` carries no migration-running code at all. Locally that is `docker compose --profile init up migrator` (or `dotnet run --project Learnix.DbMigrator`); in CI/CD it is a dedicated deploy step. `dotnet ef database update` is **not** the supported path.
2. **Seeders are plain classes implementing `IDataSeeder`** (`Learnix.DbMigrator/Seeders/`), run sequentially by the migrator. They are no longer `IHostedService` implementations inside `Learnix.Infrastructure`.
3. **Seed data is split by intent:** `System` seeders always run (Roles, Admin, Categories); `Demo` seeders (fake Courses and Students) run only behind the `--seed-demo` flag.
4. **The migrator flushes Redis when it finishes** — see ADR-BACK-INFRA-014 for why a cache must not outlive its database.

**Why:**
- **Scale-out safety.** Multiple API instances starting at once would run the startup seeders concurrently — race conditions, duplicate rows, unique-constraint violations. And auto-migration on boot means a schema error takes the API down instead of failing one deploy step.
- **Least privilege.** DDL rights (CREATE/DROP TABLE) belong to the migrator identity. The API runtime only needs DML.
- **A human review gate.** A destructive schema change should not reach production because a container restarted.
- **Startup cost.** The API boots without EF tooling, seeders or asset uploads in the path.
- **Environment control.** Production is deployed without demo content simply by omitting `--seed-demo`.

**Alternatives (this is where the previous design is recorded):**
- **Migrations in `Learnix.Infrastructure`, auto-applied by the API on startup in Development only.** This is what the project did before: an `ApplyMigrationsAsync()` extension over `IHost`, called from `Program.cs` behind `app.Environment.IsDevelopment()`, while staging and production applied migrations some other way. It bought a shorter dev loop — `docker compose up -d` + `dotnet run` and the schema was there. **Rejected**, and the extension has since been deleted: it kept two different mechanisms alive for the same job, so the path a developer exercised every day was never the path production took. A migration that only fails under the CI/CD mechanism is a migration nobody tested. One path in every environment is worth the extra `docker compose --profile init up migrator` step.
- **Seeders as `IHostedService` inside `Learnix.Infrastructure`.** Also the previous design (`CourseSeederHostedService`, `StudentSeederHostedService`). **Rejected:** on scale-out every API replica runs them at once — duplicate rows and unique-constraint violations — and it forces the API to carry seed assets and blob-upload code it never uses at runtime.
- **Always auto-migrate, in every environment.** **Rejected** — the same scale-out race, plus a schema error takes the API down instead of failing one deploy step, and destructive changes reach production without a review gate.
- **`dotnet ef database update` in CI.** Requires EF tooling on the runner and cannot seed.
- **An idempotent SQL script (`dotnet ef migrations script --idempotent`) applied with `psql`.** A legitimate approach, but it needs `psql` on the runner and an artifact to manage. More moving parts for no gain here.
- **`Database.EnsureCreatedAsync()`.** Incompatible with migrations; only usable for throwaway test databases.

**Consequences:**
- A new project to maintain in the solution, plus a small amount of duplicated DI wiring for `ApplicationDbContext`.
- After a `git pull` that brings a new migration, the database is **not** updated by running the API. The migrator has to be run.
- `Learnix.Infrastructure` owns the migration *files* (`Persistence/EntityFramework/Migrations/`, the `--output-dir` of `dotnet ef migrations add`) but nothing that applies them.

---

## ADR-BACK-MIGR-002: Seed assets are embedded resources in the migrator, and every seeded entity gets its own blob

**Context:** seeding courses, lessons and avatars needs real image/video files, and those files need to
live somewhere that doesn't depend on the host file system or the current working directory.

**Decision:** the images and videos needed to seed courses, lessons and avatars are embedded in the **`Learnix.DbMigrator`** assembly (`Learnix.DbMigrator/Assets/`, declared as `<EmbeddedResource>` in the `.csproj`, read via `Assembly.GetExecutingAssembly().GetManifestResourceStream("Learnix.DbMigrator.Assets.{name}")`). On upload, **each** seeded course or video lesson receives its own copy in Blob Storage under a unique `blobPath`.

**Why:**
- **Environment independence.** The seeder does not depend on the host file system or the current working directory — both of which are unreliable in Docker and in tests.
- **The assets belong with the seeders.** They exist for one purpose, and that purpose left `Learnix.Infrastructure` along with the seeders (ADR-BACK-MIGR-001).
- **A shared blob is a deletion hazard.** If every seeded lesson pointed at one `placeholder.mp4`, deleting a video in a single lesson would enqueue an Outbox `DeleteBlob` (ADR-BACK-BLOB-003) and destroy the file every other lesson is still serving. Unique copies make deletion safe.

**Alternatives:**
- **The same embedded resources, but in `Learnix.Infrastructure`.** The previous design, read by `CourseSeederHostedService` / `StudentSeederHostedService`. **Rejected** for the same reason the seeders themselves moved (ADR-BACK-MIGR-001): the API would keep shipping several megabytes of course covers and a placeholder video it never opens.
- **Base64 constants in C# (the design before that).** **Rejected:** enormous literals, unreadable files.
- **Reading from the file system (`File.ReadAllBytes`).** **Rejected:** depends on `Copy to Output Directory` and on the working directory; breaks in a container.
- **One shared blob per asset type.** **Rejected** — see the deletion hazard above.

**Consequences:**
- Adding a new seed asset means dropping the file into `Learnix.DbMigrator/Assets/` and registering it as an `<EmbeddedResource>` (the `.csproj` globs `*.png`, `*.webp`, `*.mp4`).
- Seeding uploads N copies of the placeholder video rather than one. Storage in the demo environment is cheap; a shared-blob 404 is not.

---

## ADR-BACK-MIGR-003: Functions and triggers are repeatable scripts, not versioned migrations

**Context:** some database objects — functions, triggers, deferrable constraints — cannot be expressed by
an EF Core migration at all, and still need a home that both the migrator and the integration-test
bootstrap can apply.

**Decision:** database objects EF Core does not model — PL/pgSQL functions, triggers, views, and
constraints EF cannot express — live as idempotent SQL in
`Learnix.Infrastructure/Persistence/EntityFramework/DatabaseObjects/*.sql`, embedded in the Infrastructure
assembly and re-applied by `DatabaseObjectsApplier` on **every** run, right after `MigrateAsync()`. They
are never put in an EF migration.

The applier lives in `Learnix.Infrastructure` (registered by `AddPersistence`), not in the migrator,
because two components stand up the schema and both need these objects: the `Learnix.DbMigrator` (dev and
CI/CD) and the integration-test bootstrap (`LearnixApp`), which applies migrations with `Migrate()` alone.
A migrator-only applier would leave the test database without them.

Each script must be safe to run against a database that already has the object: `CREATE OR REPLACE
FUNCTION`, `DROP TRIGGER IF EXISTS` before `CREATE TRIGGER`, `DROP CONSTRAINT IF EXISTS` before `ADD
CONSTRAINT`.

**Why — this is not hypothetical:**

The outbox `LISTEN/NOTIFY` optimization (ADR-BACK-OUTBOX-002 in `OUTBOX.md`) needs a trigger on `OutboxMessages` that
fires `pg_notify('outbox_new')`. `OutboxNotificationListener` shipped and held a dedicated PostgreSQL
connection open, listening on that channel. **The trigger existed in no database.** No migration created
it — verified against a live one: zero user triggers, no `notify_outbox_insert` function. The outbox kept
working, because the 10-second polling fallback carried every message, which is exactly why nobody
noticed for months that the push path was listening to a channel nobody published on.

A versioned migration would not have prevented a recurrence. It states the object **once**, in a file
that the next squash of the migration history collapses away — and the object silently stops existing in
every database created after that. That is the most likely explanation of how it was lost the first time.
The same class of object (a trigger, a function, a view) has no version to migrate *between*: its current
definition is its whole truth. Re-applying it costs one statement and cannot be lost, because it is not
part of the history being squashed.

The second object of this kind is the **deferrable uniqueness** on the ordered collections
(`ordering_deferrable.sql`). Sections within a course, and lessons within a section, carry a compact
`(parent, DisplayOrder)` unique constraint. Reordering is a permutation, which transiently puts two rows
on the same order — rejected the instant an immediate unique constraint is checked per-row. Postgres can
defer that check to `COMMIT` (`DEFERRABLE INITIALLY DEFERRED`), which makes a plain EF `SaveChanges` over
the reordered entities succeed. But EF cannot model it: a unique *index* cannot be deferrable (Postgres
defers only *constraints*), and a unique *constraint* is modelled as an alternate key, whose columns EF
then refuses to let you mutate — which is exactly what a reorder does. So EF leaves `(parent, DisplayOrder)`
unconstrained and this script owns it, squash-proof by the same logic as the trigger.

**Alternatives:**
- **An EF migration with `migrationBuilder.Sql(...)`.** Correct once, then a squash away from wrong. It
  also files a repeatable object under a mechanism built for irreversible schema deltas.
- **`context.Database.ExecuteSqlRaw` at API startup.** Puts DDL rights back in the API runtime, which
  ADR-BACK-MIGR-001 deliberately took away.
- **Applied by hand, once, per environment.** How the trigger came to exist in somebody's dev database
  and nowhere else. A step no one repeats is a step that will be missed.

**Consequences:**
- A new function/trigger/view/constraint = a new `.sql` file in
  `Learnix.Infrastructure/Persistence/EntityFramework/DatabaseObjects/`. The Infrastructure `.csproj` globs
  them as `<EmbeddedResource>`; the applier orders them by file name and executes them in that order.
- Every script runs on every apply (migrator and integration-test bootstrap), so it must be idempotent and
  cheap. If one ever isn't, it belongs in a versioned migration and is not a repeatable object.
- The DDL is checked into the repository and applied by the same component in dev, CI/CD and tests — a
  database that component has touched has the objects, whatever happened to its migration history.
- A column made unique this way is deliberately **not** modelled by EF. Document it in the entity's
  configuration (as `SectionConfiguration`/`LessonConfiguration` do) so the omission reads as intentional.

---

## ADR-BACK-MIGR-004: The migration history is squashed to a single `InitialCreate`

**Context:** EF replays the entire migration history, in order, against every fresh database, and that
history had grown long enough to be worth collapsing.

**Decision:** the full migration history was squashed into one `InitialCreate`, scaffolded from the
current model.

**Why it was safe:** the regenerated model snapshot was byte-identical to the old one (worth diffing, not
assuming); the only raw SQL in the chain was backfill for rows a fresh database lacks; and the objects EF
cannot model were never in a migration to begin with (ADR-BACK-MIGR-003).

**Consequences:** existing databases are recreated rather than migrated — `docker compose down -v`, then
the migrator. Cheap here because the deployment carries only seeded demo data.
