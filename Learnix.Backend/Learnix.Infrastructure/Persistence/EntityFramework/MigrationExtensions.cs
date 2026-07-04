using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Persistence.EntityFramework;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations. Intended for development only —
    /// in production, migrations should be applied as a controlled deployment step.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILogger<ApplicationDbContext>>();
        var db = sp.GetRequiredService<ApplicationDbContext>();

        try
        {
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed. Application will not start.");
            throw new InvalidOperationException("Database migration failed.", ex);
        }
    }
}
