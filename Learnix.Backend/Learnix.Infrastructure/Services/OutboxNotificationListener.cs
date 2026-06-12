using Learnix.Infrastructure.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Learnix.Infrastructure.Services;

/// <summary>
/// Maintains a dedicated PostgreSQL connection and listens for <c>outbox_new</c>
/// notifications emitted by the <c>trg_outbox_notify</c> trigger on <c>OutboxMessages</c>.
/// On each notification, signals <see cref="OutboxSignal"/> to wake the
/// <see cref="OutboxProcessorService"/> immediately.
/// <para>
/// The connection is <b>not pooled</b> — PostgreSQL LISTEN state is tied to a session
/// and would be lost when the connection returns to the pool.
/// </para>
/// </summary>
internal sealed class OutboxNotificationListener(
    IConfiguration configuration,
    OutboxSignal outboxSignal,
    ILogger<OutboxNotificationListener> logger) : BackgroundService
{
    private const string Channel = "outbox_new";
    private static readonly TimeSpan InitialReconnectDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        var reconnectDelay = InitialReconnectDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(stoppingToken);

                connection.Notification += OnNotification;

                await using (var cmd = new NpgsqlCommand($"LISTEN {Channel}", connection))
                {
                    await cmd.ExecuteNonQueryAsync(stoppingToken);
                }

                logger.LogInformation("Listening on PostgreSQL channel '{Channel}'.", Channel);
                reconnectDelay = InitialReconnectDelay;

                // Block until a notification arrives or cancellation is requested.
                // WaitAsync returns after each notification; loop to keep listening.
                while (!stoppingToken.IsCancellationRequested)
                {
                    await connection.WaitAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "PostgreSQL LISTEN connection lost. Reconnecting in {Delay}s...",
                    reconnectDelay.TotalSeconds);

                try
                {
                    await Task.Delay(reconnectDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                // Exponential backoff capped at MaxReconnectDelay
                reconnectDelay = TimeSpan.FromSeconds(
                    Math.Min(reconnectDelay.TotalSeconds * 2, MaxReconnectDelay.TotalSeconds));
            }
        }

        logger.LogInformation("Outbox notification listener stopped.");
    }

    private void OnNotification(object sender, NpgsqlNotificationEventArgs e)
    {
        outboxSignal.Notify();
    }
}
