namespace Learnix.Infrastructure.Outbox;

/// <summary>
/// In-process signal that wakes the <see cref="Services.OutboxProcessorService"/>
/// when new outbox messages are available. Registered as singleton.
/// <para>
/// The <see cref="Services.OutboxNotificationListener"/> calls <see cref="Notify"/>
/// when a PostgreSQL LISTEN/NOTIFY notification arrives on the <c>outbox_new</c> channel.
/// The processor awaits <see cref="WaitAsync"/> which returns immediately on signal
/// or after the fallback timeout — whichever comes first.
/// </para>
/// </summary>
internal sealed class OutboxSignal
{
    private readonly SemaphoreSlim _semaphore = new(0);

    /// <summary>Signal the processor that new messages are available.</summary>
    public void Notify() => _semaphore.Release();

    /// <summary>
    /// Wait until signalled or <paramref name="timeout"/> expires.
    /// Returns <c>true</c> if signalled, <c>false</c> on timeout.
    /// </summary>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct)
        => _semaphore.WaitAsync(timeout, ct);
}
