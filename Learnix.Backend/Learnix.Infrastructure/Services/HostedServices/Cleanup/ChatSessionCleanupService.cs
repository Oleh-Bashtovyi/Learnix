using Learnix.Application.AiChat.Abstractions;
using Learnix.Infrastructure.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services.HostedServices.Cleanup;

internal sealed class ChatSessionCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ChatSessionCleanupService> logger)
    : ScheduledBackgroundService
{
    protected override TimeSpan Interval => BackgroundJobConstants.ChatSessionCleanupInterval;

    protected override async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();

            var threshold = DateTime.UtcNow.Subtract(BackgroundJobConstants.ChatSessionRetentionPeriod);
            await repository.DeleteOlderThanAsync(threshold, ct);

            logger.LogInformation(
                "Chat session cleanup completed (threshold: sessions closed before {Threshold:u}).",
                threshold);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Chat session cleanup failed.");
        }
    }
}
