using Learnix.Infrastructure.Persistence.Mongo.Documents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Learnix.Infrastructure.Persistence.Mongo;

internal sealed class MongoIndexInitializer(
    MongoDbContext context,
    ILogger<MongoIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var indexKeys = Builders<ChatSessionDocument>.IndexKeys
                .Ascending(s => s.UserId)
                .Ascending(s => s.IsActive);

            var indexModel = new CreateIndexModel<ChatSessionDocument>(
                indexKeys,
                new CreateIndexOptions { Name = "IX_chat_sessions_userId_isActive" });

            await context.ChatSessions.Indexes.CreateOneAsync(indexModel, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure MongoDB indexes. Continuing startup.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
