using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Domain.Enums;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Outbox.Payloads;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;
using Learnix.Infrastructure.Outbox.Payloads.Notifications;
using Learnix.Infrastructure.Outbox.Payloads.Users;
using Learnix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Learnix.Infrastructure.Services;

internal sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    OutboxSignal outboxSignal,
    ILogger<OutboxProcessorService> logger)
    : BackgroundService
{
    private static readonly TimeSpan FallbackInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wake on LISTEN/NOTIFY signal OR after fallback timeout (whichever comes first)
                await outboxSignal.WaitAsync(FallbackInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessBatchAsync(stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
            var achievementEvaluator = scope.ServiceProvider.GetRequiredService<IAchievementEvaluator>();
            var achievementNotifier = scope.ServiceProvider.GetRequiredService<IAchievementNotifier>();
            var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

            // Add a 1-second buffer to account for PostgreSQL timestamp rounding.
            // .NET DateTime has 100ns precision, while PostgreSQL has 1us precision.
            // PostgreSQL can round up the NextRetryAt timestamp, causing it to be slightly
            // in the future compared to the next immediate poll's DateTime.UtcNow.
            var now = DateTime.UtcNow.AddSeconds(1);

            // Explicit transaction: FOR UPDATE locks are held until COMMIT,
            // preventing other instances from picking the same messages.
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            // FOR UPDATE SKIP LOCKED — row-level distributed lock:
            // if another instance already locked a row, skip it instead of waiting.
            // No LINQ composition after FromSqlRaw → EF passes SQL directly (no subquery wrapping).
            var messages = await db.OutboxMessages
                .FromSqlRaw(@"
                    SELECT * FROM ""OutboxMessages""
                    WHERE ""ProcessedAt"" IS NULL AND ""NextRetryAt"" <= {0}
                    ORDER BY ""OccurredAt""
                    LIMIT {1}
                    FOR UPDATE SKIP LOCKED", now, BatchSize)
                .ToListAsync(ct);

            foreach (var message in messages)
            {
                try
                {
                    await DispatchAsync(message, emailSender, blobStorage, achievementEvaluator, achievementNotifier, notificationSender, ct);
                    message.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to process outbox message {MessageId} (type={Type}, attempt={Attempt}).",
                        message.Id, message.Type, message.AttemptCount + 1);

                    message.AttemptCount++;
                    message.LastAttemptAt = DateTime.UtcNow;
                    message.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;

                    // Exponential backoff: 10s * 2^attempt, capped at 1 hour
                    var delaySeconds = Math.Min(10 * Math.Pow(2, message.AttemptCount), 3600);
                    message.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
                }

                await db.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);

            // If we processed any messages, there might be more (either from a full batch,
            // or new ones generated during processing of this batch).
            // Signal self to check again immediately.
            if (messages.Count > 0)
                outboxSignal.Notify();
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Outbox processor batch failed.");
        }
    }

    private static async Task DispatchAsync(
        OutboxMessage message,
        IEmailSender emailSender,
        IBlobStorageService blobStorage,
        IAchievementEvaluator achievementEvaluator,
        IAchievementNotifier achievementNotifier,
        INotificationSender notificationSender,
        CancellationToken ct)
    {
        switch (message.Type)
        {
            case OutboxMessageTypes.InstructorApprovedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendInstructorApprovedEmailPayload>(message.Payload)!;
                await emailSender.SendInstructorApplicationApprovedAsync(payload.ToEmail, payload.FirstName, payload.Language, ct);
                break;
            }
            case OutboxMessageTypes.InstructorRejectedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendInstructorRejectedEmailPayload>(message.Payload)!;
                await emailSender.SendInstructorApplicationRejectedAsync(payload.ToEmail, payload.FirstName, payload.RejectionReason, payload.Language, ct);
                break;
            }
            case OutboxMessageTypes.DeleteBlob:
            {
                var payload = JsonSerializer.Deserialize<DeleteBlobPayload>(message.Payload)!;
                await blobStorage.DeleteAsync(payload.BlobPath, ct);
                break;
            }
            case OutboxMessageTypes.MarkBlobConfirmed:
            {
                var payload = JsonSerializer.Deserialize<MarkBlobConfirmedPayload>(message.Payload)!;
                await blobStorage.MarkConfirmedAsync(payload.BlobPath, ct);
                break;
            }
            case OutboxMessageTypes.UserBannedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendUserBannedEmailPayload>(message.Payload)!;
                await emailSender.SendUserBannedAsync(payload.ToEmail, payload.FirstName, payload.Language, ct);
                break;
            }
            case OutboxMessageTypes.UserUnbannedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendUserUnbannedEmailPayload>(message.Payload)!;
                await emailSender.SendUserUnbannedAsync(payload.ToEmail, payload.FirstName, payload.Language, ct);
                break;
            }
            case OutboxMessageTypes.UserRoleChangedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendUserRoleChangedEmailPayload>(message.Payload)!;
                await emailSender.SendUserRoleChangedAsync(payload.ToEmail, payload.FirstName, payload.Role, payload.Assigned, payload.Language, ct);
                break;
            }
            case OutboxMessageTypes.CourseAdminUnpublishedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendCourseAdminActionEmailPayload>(message.Payload)!;
                await emailSender.SendCourseAdminUnpublishedAsync(payload.ToEmail, payload.InstructorFirstName, payload.CourseTitle, payload.Language, ct);
                break;
            }
            case OutboxMessageTypes.CourseAdminDeletedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendCourseAdminActionEmailPayload>(message.Payload)!;
                await emailSender.SendCourseAdminDeletedAsync(payload.ToEmail, payload.InstructorFirstName, payload.CourseTitle, payload.Language, ct);
                break;
            }
            case OutboxMessageTypes.EvaluateLessonCompleted:
            {
                var payload = JsonSerializer.Deserialize<EvaluateLessonCompletedPayload>(message.Payload)!;
                await achievementEvaluator.OnLessonCompletedAsync(payload.UserId, ct);
                break;
            }
            case OutboxMessageTypes.EvaluateEnrollmentCompleted:
            {
                var payload = JsonSerializer.Deserialize<EvaluateEnrollmentCompletedPayload>(message.Payload)!;
                await achievementEvaluator.OnEnrollmentCompletedAsync(payload.UserId, payload.CourseId, ct);
                break;
            }
            case OutboxMessageTypes.EvaluateTestSubmitted:
            {
                var payload = JsonSerializer.Deserialize<EvaluateTestSubmittedPayload>(message.Payload)!;
                await achievementEvaluator.OnTestSubmittedAsync(
                    payload.UserId, payload.QuestionsCount, payload.DurationSeconds, payload.Passed, ct);
                break;
            }
            case OutboxMessageTypes.EvaluateProfileChanged:
            {
                var payload = JsonSerializer.Deserialize<EvaluateProfileChangedPayload>(message.Payload)!;
                await achievementEvaluator.OnProfileChangedAsync(payload.UserId, ct);
                break;
            }
            case OutboxMessageTypes.NotifyAchievementUnlocked:
            {
                var payload = JsonSerializer.Deserialize<NotifyAchievementUnlockedPayload>(message.Payload)!;
                await achievementNotifier.NotifyAsync(
                    payload.UserId, payload.UserAchievementId, payload.Code, payload.UnlockedAt, ct);
                await notificationSender.SendAsync(
                    payload.UserId,
                    NotificationType.AchievementEarned,
                    "Achievement Unlocked",
                    $"You've earned a new achievement: {payload.Code.Replace('_', ' ')}",
                    ct);
                break;
            }
            case OutboxMessageTypes.NotifyInstructorApproved:
            {
                var payload = JsonSerializer.Deserialize<NotifyInstructorApprovedPayload>(message.Payload)!;
                await notificationSender.SendAsync(
                    payload.UserId,
                    NotificationType.InstructorApproved,
                    "Application Approved",
                    "Your instructor application has been approved. Welcome aboard!",
                    ct);
                break;
            }
            case OutboxMessageTypes.NotifyInstructorRejected:
            {
                var payload = JsonSerializer.Deserialize<NotifyInstructorRejectedPayload>(message.Payload)!;
                await notificationSender.SendAsync(
                    payload.UserId,
                    NotificationType.InstructorRejected,
                    "Application Rejected",
                    "Your instructor application was not approved at this time.",
                    ct);
                break;
            }
            default:
                throw new InvalidOperationException($"Unknown outbox message type: {message.Type}");
        }
    }
}
