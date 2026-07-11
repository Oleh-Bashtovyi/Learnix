using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Learnix.DbMigrator.Seeders;

/// <summary>
/// Empties the Redis cache after the database has been migrated and seeded.
///
/// <para>
/// The cache outlives the database, and that is a trap. Drop or re-seed PostgreSQL and every id changes —
/// but <c>categories:all</c> still sits in Redis for the rest of its 24-hour TTL, so the catalog offers
/// categories whose ids no longer exist and filtering by one of them returns nothing at all. Nothing is
/// broken in the code; the two stores simply disagree about which world they are in, and the cache wins for
/// a day.
/// </para>
///
/// <para>
/// Every key in Redis is derived data — cached queries, and the AI provider's last known outage — so
/// throwing all of it away costs a few cold reads and nothing else. It is the only safe move: keys cannot be
/// enumerated by prefix through <c>IDistributedCache</c>, and guessing which ones went stale is exactly the
/// kind of bookkeeping that rots.
/// </para>
/// </summary>
public sealed class RedisCacheFlusher(IConfiguration configuration, ILogger<RedisCacheFlusher> logger)
{
    public async Task FlushAsync(CancellationToken ct = default)
    {
        var connectionString = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogInformation("No Redis connection string configured — nothing to flush.");
            return;
        }

        try
        {
            // FLUSHDB is an admin command, and the client refuses to send one without being told to.
            var options = ConfigurationOptions.Parse(connectionString);
            options.AllowAdmin = true;

            await using var redis = await ConnectionMultiplexer.ConnectAsync(options);

            foreach (var endpoint in redis.GetEndPoints())
            {
                var server = redis.GetServer(endpoint);

                // A replica has nothing of its own to forget; flushing it would fail anyway.
                if (server.IsReplica)
                    continue;

                await server.FlushDatabaseAsync();
                logger.LogInformation("Flushed the Redis cache on {Endpoint}.", endpoint);
            }
        }
        catch (Exception ex)
        {
            // The migration itself has already succeeded. A cache we could not reach is a stale cache, not a
            // failed deployment — say so loudly and let the run finish.
            logger.LogWarning(
                ex,
                "Could not flush the Redis cache. Entries written against the previous database may serve "
                + "ids that no longer exist until their TTL expires.");
        }
    }
}
