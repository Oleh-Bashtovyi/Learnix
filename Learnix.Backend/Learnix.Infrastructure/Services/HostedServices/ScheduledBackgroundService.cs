using Microsoft.Extensions.Hosting;

namespace Learnix.Infrastructure.Services.HostedServices;

/// <summary>
/// Base class for background services that run periodically.
/// </summary>
internal abstract class ScheduledBackgroundService : BackgroundService
{
    /// <summary>
    /// The interval at which the background service should execute.
    /// </summary>
    protected abstract TimeSpan Interval { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        // Run immediately on startup
        await RunAsync(stoppingToken);

        // Then run on every tick
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAsync(stoppingToken);
        }
    }

    /// <summary>
    /// The logic to execute on each interval tick. Any necessary scoping (e.g. creating an IServiceScope)
    /// and exception handling should be implemented within this method to prevent the background service from terminating.
    /// </summary>
    protected abstract Task RunAsync(CancellationToken stoppingToken);
}
