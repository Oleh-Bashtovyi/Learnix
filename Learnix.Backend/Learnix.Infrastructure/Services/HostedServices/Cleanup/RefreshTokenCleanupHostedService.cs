using Learnix.Application.Common.Constants;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services.HostedServices.Cleanup;

internal sealed class RefreshTokenCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupHostedService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = BlobUrlTtlConstants.CertificateReadUrl;
    private static readonly TimeSpan RetentionAfterExpiry = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        await RunAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunAsync(stoppingToken);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoff = DateTime.UtcNow.Subtract(RetentionAfterExpiry);

            var deleted = await context.RefreshTokens
                .Where(t => t.ExpiresAt < cutoff)
                .ExecuteDeleteAsync(ct);

            if (deleted > 0)
                logger.LogInformation(
                    "Refresh token cleanup: removed {Count} expired tokens (cutoff {Cutoff:u}).",
                    deleted, cutoff);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Refresh token cleanup failed.");
        }
    }
}