using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Outbox.Payloads;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;
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
    ILogger<OutboxProcessorService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ProcessBatchAsync(stoppingToken);
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

            var messages = await db.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.NextRetryAt <= DateTime.UtcNow)
                .OrderBy(m => m.OccurredAt)
                .Take(BatchSize)
                .ToListAsync(ct);

            foreach (var message in messages)
            {
                try
                {
                    await DispatchAsync(message, emailSender, blobStorage, achievementEvaluator, achievementNotifier, ct);
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
        CancellationToken ct)
    {
        switch (message.Type)
        {
            case OutboxMessageTypes.InstructorApprovedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendInstructorApprovedEmailPayload>(message.Payload)!;
                await emailSender.SendInstructorApplicationApprovedAsync(payload.ToEmail, payload.FirstName, ct);
                break;
            }
            case OutboxMessageTypes.InstructorRejectedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendInstructorRejectedEmailPayload>(message.Payload)!;
                await emailSender.SendInstructorApplicationRejectedAsync(payload.ToEmail, payload.FirstName, payload.RejectionReason, ct);
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
                await emailSender.SendUserBannedAsync(payload.ToEmail, payload.FirstName, ct);
                break;
            }
            case OutboxMessageTypes.UserUnbannedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendUserUnbannedEmailPayload>(message.Payload)!;
                await emailSender.SendUserUnbannedAsync(payload.ToEmail, payload.FirstName, ct);
                break;
            }
            case OutboxMessageTypes.UserRoleChangedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendUserRoleChangedEmailPayload>(message.Payload)!;
                await emailSender.SendUserRoleChangedAsync(payload.ToEmail, payload.FirstName, payload.Role, payload.Assigned, ct);
                break;
            }
            case OutboxMessageTypes.CourseAdminUnpublishedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendCourseAdminActionEmailPayload>(message.Payload)!;
                await emailSender.SendCourseAdminUnpublishedAsync(payload.ToEmail, payload.InstructorFirstName, payload.CourseTitle, ct);
                break;
            }
            case OutboxMessageTypes.CourseAdminDeletedEmail:
            {
                var payload = JsonSerializer.Deserialize<SendCourseAdminActionEmailPayload>(message.Payload)!;
                await emailSender.SendCourseAdminDeletedAsync(payload.ToEmail, payload.InstructorFirstName, payload.CourseTitle, ct);
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
                break;
            }
            default:
                throw new InvalidOperationException($"Unknown outbox message type: {message.Type}");
        }
    }
}
