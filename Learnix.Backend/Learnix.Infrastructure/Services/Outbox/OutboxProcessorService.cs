using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services.Outbox;

/// <summary>
/// Drains the outbox: takes a batch under a row lock, hands each message to the handler that owns its type,
/// and retries with backoff whatever fails. It knows nothing about emails, achievements or blobs — that is
/// <see cref="IOutboxMessageDispatcher"/>'s business (ADR-INFRA-013).
/// </summary>
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
            var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxMessageDispatcher>();

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
                    await dispatcher.DispatchAsync(message, ct);
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
}
