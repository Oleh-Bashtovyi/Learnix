using Learnix.Infrastructure.Constants;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services.HostedServices.Cleanup;

internal sealed class RefreshTokenCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupHostedService> logger)
    : ScheduledBackgroundService
{
    protected override TimeSpan Interval => BackgroundJobConstants.RefreshTokenCleanupInterval;

    protected override async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoff = DateTime.UtcNow.Subtract(BackgroundJobConstants.RefreshTokenRetentionAfterExpiry);

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
