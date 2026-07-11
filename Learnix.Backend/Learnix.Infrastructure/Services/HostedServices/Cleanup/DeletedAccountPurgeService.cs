using Learnix.Application.AiChat.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Constants;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services.HostedServices.Cleanup;

/// <summary>
/// Erases the personal data of accounts whose recovery window has run out — the promise the deletion email
/// makes, kept (<c>User.PurgeAfter</c>).
///
/// <para>
/// <b>It anonymizes; it does not delete the row. That is not laziness — the schema forbids the alternative.</b>
/// A hard <c>DELETE</c> of an <c>AspNetUsers</c> row today does one of three things, depending on who the user
/// was:
/// </para>
///
/// <para>
/// <b>1. It fails.</b> <c>CourseReviews.StudentId</c>, <c>CourseConversations.StudentId</c> /
/// <c>InstructorId</c>, <c>CourseMessages.SenderId</c> and <c>InstructorApplications.ReviewedByAdminId</c> are
/// all <c>ON DELETE RESTRICT</c>. Anyone who ever wrote a review or a single message cannot be deleted at all,
/// and the job would die on the FK violation. Cascading them instead is not an option either: a review is part
/// of a course's rating, and a message is half of somebody else's conversation. Deleting a person's account is
/// not a licence to rewrite what other people see.
/// </para>
///
/// <para>
/// <b>2. It destroys records that are not only theirs.</b> <c>Payments.UserId</c> is <c>ON DELETE CASCADE</c>,
/// so deleting the user would take their payment history with it — the same rows an instructor's earnings and
/// the admin ledger are built from. Financial records outlive the account by design. <b>This is the one that
/// needs a decision:</b> either the FK becomes <c>RESTRICT</c> with the payment kept and de-identified (it
/// already carries its own amounts and ids), or payments move to a store that is not keyed by the user at all.
/// Until then, purging must never touch them.
/// </para>
///
/// <para>
/// <b>3. It silently orphans the rest.</b> <c>Enrollments</c>, <c>Certificates</c>, <c>LessonProgress</c>,
/// <c>TestAttempts</c> and <c>Courses</c> carry a <c>Guid</c> for the user and <b>no foreign key at all</b>.
/// Nothing would stop the delete and nothing would clean them up: a certificate whose public verification page
/// can no longer name its holder, a course whose instructor does not exist while its students are still
/// enrolled. The missing constraints are a schema gap in their own right, and they are why "just delete it"
/// looks harmless right up until it isn't.
/// </para>
///
/// <para>
/// So: the row survives, stripped of the person (<see cref="User.Anonymize"/>), and everything that is purely
/// theirs is deleted outright. See <c>docs/TECH_DEBT.md</c> for the outstanding decisions.
/// </para>
/// </summary>
internal sealed class DeletedAccountPurgeService(
    IServiceScopeFactory scopeFactory,
    ILogger<DeletedAccountPurgeService> logger)
    : ScheduledBackgroundService
{
    /// <summary>One batch per run: a purge is never urgent, and a runaway loop over users would not be free.</summary>
    private const int BatchSize = 100;

    protected override TimeSpan Interval => BackgroundJobConstants.DeletedAccountPurgeInterval;

    protected override async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var chatSessions = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();

            var now = DateTime.UtcNow;

            // The users are soft-deleted, so the global filter hides the very rows this job exists for.
            var due = await context.Set<User>()
                .IgnoreQueryFilters()
                .Where(u => u.IsDeleted && u.PurgeAfter != null && u.PurgeAfter <= now)
                .OrderBy(u => u.PurgeAfter)
                .Take(BatchSize)
                .ToListAsync(stoppingToken);

            if (due.Count == 0)
                return;

            foreach (var user in due)
            {
                await PurgePersonalRecordsAsync(context, chatSessions, user.Id, stoppingToken);

                // Clears PurgeAfter, so a user cannot be purged twice, and raises the avatar-removed event
                // whose outbox message deletes the blob.
                user.Anonymize();
            }

            await context.SaveChangesAsync(stoppingToken);

            logger.LogInformation(
                "Deleted account purge: anonymized {Count} accounts whose recovery window expired.",
                due.Count);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Deleted account purge failed.");
        }
    }

    /// <summary>
    /// Everything that belongs to this user and to nobody else. What is left out is left out on purpose:
    /// payments (someone else's books), reviews and messages (someone else's course and thread), enrollments,
    /// progress, attempts and certificates (the record that a person passed, which survives them anonymously).
    /// </summary>
    private static async Task PurgePersonalRecordsAsync(
        ApplicationDbContext context,
        IChatSessionRepository chatSessions,
        Guid userId,
        CancellationToken stoppingToken)
    {
        await context.RefreshTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync(stoppingToken);
        await context.Notifications.Where(n => n.UserId == userId).ExecuteDeleteAsync(stoppingToken);
        await context.WishlistItems.Where(w => w.UserId == userId).ExecuteDeleteAsync(stoppingToken);
        await context.UserAchievements.Where(a => a.UserId == userId).ExecuteDeleteAsync(stoppingToken);
        await context.UserAchievementProgresses.Where(p => p.UserId == userId).ExecuteDeleteAsync(stoppingToken);
        await context.UserCompletedCategories.Where(c => c.UserId == userId).ExecuteDeleteAsync(stoppingToken);

        // The Google link and any Identity-issued tokens: credentials, not history.
        await context.Set<IdentityUserLogin<Guid>>().Where(l => l.UserId == userId).ExecuteDeleteAsync(stoppingToken);
        await context.Set<IdentityUserToken<Guid>>().Where(t => t.UserId == userId).ExecuteDeleteAsync(stoppingToken);
        await context.Set<IdentityUserClaim<Guid>>().Where(c => c.UserId == userId).ExecuteDeleteAsync(stoppingToken);

        // Chat history lives in Mongo and no cascade reaches it.
        await chatSessions.DeleteAllForUserAsync(userId, stoppingToken);
    }
}
