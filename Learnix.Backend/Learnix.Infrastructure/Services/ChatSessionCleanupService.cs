using Learnix.Application.Common.Constants;
using Learnix.Application.AiChat.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services;

internal sealed class ChatSessionCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ChatSessionCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = BlobUrlTtlConstants.CertificateReadUrl;
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

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
            var repository = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();

            var threshold = DateTime.UtcNow.Subtract(RetentionPeriod);
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
