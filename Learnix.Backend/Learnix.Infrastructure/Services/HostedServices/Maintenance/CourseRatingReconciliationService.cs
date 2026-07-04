using Learnix.Infrastructure.Constants;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services.HostedServices.Maintenance;

// WHY THIS SERVICE EXISTS — THE FULL PICTURE
//
// The Course aggregate maintains denormalized ReviewsCount and AverageRating fields.
// While our CQRS handlers (CreateReview/UpdateReview/DeleteReview) wrap database updates
// in a single transaction and pull the exact counts via correlated subqueries to prevent
// drift from concurrent writes, there is still a risk of data drift over time due to:
//
//   1. DIRECT DATABASE MODIFICATIONS — DBAs, manual scripts, or bulk imports that INSERT/DELETE 
//      reviews directly bypassing the application logic entirely.
//
//   2. HANDLER BUGS — Future changes to handlers might inadvertently omit the synchronization 
//      step, causing silent drift.
//
// This reconciliation job acts as an ultimate safety net. It periodically recalculates every 
// course's rating and count directly from the CourseReviews table (the authoritative source 
// of truth) and corrects any discrepancy.
//
// WHY USE RAW SQL?
//
// Using a single raw SQL UPDATE statement with a correlated subquery is vastly more efficient 
// than loading all Courses into memory, fetching their reviews via EF Core, calculating the 
// averages in memory, and saving them back one by one. The database engine can execute this
// bulk update in milliseconds, whereas an EF-tracked loop would take significantly longer and 
// consume high memory for a large dataset.
//
// INTERVAL CHOICE — 24 hours
//
// Because the primary source of drift (race conditions) is already handled at the transaction 
// level within CQRS handlers, this job only exists for edge cases. Running it once every 24 hours
// is more than sufficient and introduces zero noticeable DB load.

internal sealed class CourseRatingReconciliationService(
    IServiceScopeFactory scopeFactory,
    ILogger<CourseRatingReconciliationService> logger)
    : ScheduledBackgroundService
{
    protected override TimeSpan Interval => BackgroundJobConstants.CourseRatingReconciliationInterval;

    protected override async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Single UPDATE: recalculates every course's review count and average rating.
            // COALESCE is used because if a course has no reviews, the subqueries will return 
            // 0 (for COUNT) and NULL (for AVG). We want them to fallback to 0.
            var affected = await db.Database.ExecuteSqlAsync(
                $"""
                UPDATE "Courses"
                SET 
                    "ReviewsCount" = (
                        SELECT COUNT(*) 
                        FROM "CourseReviews" 
                        WHERE "CourseReviews"."CourseId" = "Courses"."Id"
                    ),
                    "AverageRating" = COALESCE((
                        SELECT ROUND(AVG("CourseReviews"."Rating")::numeric, 2) 
                        FROM "CourseReviews" 
                        WHERE "CourseReviews"."CourseId" = "Courses"."Id"
                    ), 0)
                WHERE "DeletedAt" IS NULL
                """, ct);

            logger.LogInformation(
                "Course rating reconciliation complete. Rows updated: {Affected}.", affected);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Course rating reconciliation failed.");
        }
    }
}
