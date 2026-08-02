using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Persistence.EntityFramework.DatabaseObjects;

/// <summary>
/// Applies the database objects that EF Core does not model — functions, triggers, and the deferrable
/// ordering constraints — from the embedded <c>DatabaseObjects/*.sql</c> scripts, after the migrations
/// have run.
/// </summary>
/// <remarks>
/// These are **repeatable**, not versioned: each script is written to be idempotent
/// (<c>CREATE OR REPLACE</c>, <c>DROP ... IF EXISTS</c>) and is re-applied on every run.
///
/// Why not an EF migration: a migration states the object once, in a file that a future squash will
/// collapse away. The outbox notify trigger was exactly that casualty — it existed in the code's
/// imagination and in no database (ADR-BACK-MIGR-003). A repeatable script cannot be lost to a squash,
/// because it is not part of the history being squashed.
///
/// It lives in Infrastructure (not the DbMigrator) so both the migrator and the integration-test
/// bootstrap can apply it — the tests stand up the schema with <c>Migrate()</c> alone and would otherwise
/// miss these objects.
/// </remarks>
public sealed class DatabaseObjectsApplier(ApplicationDbContext dbContext, ILogger<DatabaseObjectsApplier> logger)
{
    private const string ResourcePrefix = "Learnix.Infrastructure.Persistence.EntityFramework.DatabaseObjects.";

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var scripts = assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                && name.EndsWith(".sql", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (scripts.Count == 0)
        {
            logger.LogWarning("No database object scripts were found as embedded resources.");
            return;
        }

        foreach (var script in scripts)
        {
            await using var stream = assembly.GetManifestResourceStream(script)
                ?? throw new InvalidOperationException($"Embedded script '{script}' could not be opened.");

            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            logger.LogInformation("Applied database object script {Script}.", script[ResourcePrefix.Length..]);
        }
    }
}
