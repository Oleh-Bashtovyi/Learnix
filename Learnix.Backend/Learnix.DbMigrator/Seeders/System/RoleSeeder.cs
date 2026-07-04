using Learnix.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Learnix.DbMigrator.Seeders;

internal sealed class RoleSeeder(
    IServiceProvider serviceProvider,
    ILogger<RoleSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (result.Succeeded)
                    logger.LogInformation("Seeded role: {Role}", roleName);
                else
                    logger.LogError("Failed to seed role {Role}: {Errors}", roleName, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
