using Learnix.Domain.Enums;
using Learnix.Infrastructure.Constants;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services.HostedServices.Maintenance;

// WHY THIS SERVICE EXISTS — THE FULL PICTURE
//
// Category.CoursesCount is a denormalized counter: instead of running COUNT(*) on the
// Courses table every time a category list is rendered, we maintain an integer on the
// Category entity itself and update it incrementally via domain event handlers
// (CoursePublishedCountHandler → increment, CourseUnpublished/Archived/Deleted → decrement).
//
// This "event-driven counter" approach is fast and transactionally safe for the happy path,
// but it has several known failure modes that can cause the counter to drift from the truth:
//
//   1. RACE CONDITION — two requests publish courses in the same category simultaneously.
//      Both read CoursesCount = 5, both write 6. The second increment is lost.
//      Result: counter is 6 instead of 7.
//
//   2. AT-LEAST-ONCE DELIVERY — if the domain event handler is retried (e.g., transient DB
//      error on first attempt), the decrement runs twice. CoursesCount goes negative.
//      Category.DecrementCoursesCount() clamps to 0, but the count is still wrong.
//
//   3. MISSED EVENTS — if the application process crashes between SaveChangesAsync and the
//      MediatR in-process event dispatch, the count handler never runs. The entity is saved
//      but the counter is not updated. (This risk is documented in ADR-010 for Phase 6.)
//
//   4. DIRECT DATABASE MODIFICATIONS — migrations, admin hotfixes, or seed scripts that
//      INSERT/DELETE courses directly bypass the domain model entirely. No events fire.
//
//   5. HANDLER BUG — a subtle bug in WasPublished logic (e.g., forgetting to check it)
//      causes wrong increment/decrement for months before anyone notices.
//
// The reconciliation job is the safety net. It periodically recalculates every category's
// count directly from the Courses table — the authoritative source of truth — and corrects
// any discrepancy in a single SQL statement.
//
// ARCHITECTURE NOTE — WHY IHostedService AND NOT QUARTZ.NET OR HANGFIRE?
//
// Quartz.NET and Hangfire are distributed job schedulers. Their key feature is that they
// coordinate job execution across multiple application instances using a shared database lock:
// if you run 3 replicas of the API, only ONE replica executes the job at any given time.
// This matters when the job has side effects that must not run in parallel (e.g., sending
// emails, charging a payment card).
//
// For THIS specific job, running simultaneously on all replicas is acceptable:
//   - Each replica runs the same idempotent UPDATE SQL.
//   - The last writer wins; all writes produce the same correct result.
//   - PostgreSQL row-level locks prevent torn updates between concurrent UPDATEs.
//   - The worst outcome is slightly more DB load during the reconciliation window.
//
// IHostedService (BackgroundService with PeriodicTimer) is already used throughout this
// codebase (RefreshTokenCleanupHostedService, CertificatePdfGenerationService) and adds
// zero dependencies. Quartz.NET or Hangfire would be worth introducing when:
//   - A job must run exactly once across all replicas (e.g., triggering a bulk email send).
//   - Jobs need a management UI for monitoring, manual retries, or scheduling changes.
//   - The number of background jobs grows large enough that ad-hoc PeriodicTimer services
//     become hard to manage.
// See ADR-015 for the full decision record.
//
// INTERVAL CHOICE — 1 hour
//
// For an LMS, a category showing 42 instead of 43 courses for up to 1 hour is not
// user-visible harm. 1 hour is a reasonable balance between accuracy and DB load.
// If drift becomes a problem in production (e.g., after a bulk import of courses),
// the interval can be shortened or the job triggered manually.

internal sealed class CategoryCoursesCountReconciliationService(
    IServiceScopeFactory scopeFactory,
    ILogger<CategoryCoursesCountReconciliationService> logger)
    : ScheduledBackgroundService
{
    protected override TimeSpan Interval => BackgroundJobConstants.CategoryReconciliationInterval;

    protected override async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Single UPDATE: recalculates every category's count in one round trip.
            // Uses a correlated subquery to count only Published, non-deleted courses.
            // EF Core's ExecuteSqlAsync uses FormattableString interpolation which is
            // parameterized internally — safe from SQL injection despite the inline value.
            var affected = await db.Database.ExecuteSqlAsync(
                $"""
                UPDATE "Categories"
                SET "CoursesCount" = (
                    SELECT COUNT(*)
                    FROM "Courses"
                    WHERE "Courses"."CategoryId" = "Categories"."Id"
                      AND "Courses"."Status" = {(int)CourseStatus.Published}
                      AND "Courses"."DeletedAt" IS NULL
                )
                """, ct);

            logger.LogInformation(
                "Category courses count reconciliation complete. Rows updated: {Affected}.", affected);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Category courses count reconciliation failed.");
        }
    }
}
