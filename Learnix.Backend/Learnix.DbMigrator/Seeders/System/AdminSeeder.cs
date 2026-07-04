using Learnix.DbMigrator.Constants;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Learnix.DbMigrator.Seeders;

internal sealed class AdminSeeder(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<AdminSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration[$"{ConfigurationSectionNameConstants.SeedAdmin}:Email"];
        var password = configuration[$"{ConfigurationSectionNameConstants.SeedAdmin}:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                $"SeedAdmin:Email or {ConfigurationSectionNameConstants.SeedAdmin}:Password is not configured — skipping admin seeding. " +
                "Set both values to create the initial admin account on startup.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var usersInAdminRole = await userManager.GetUsersInRoleAsync(Roles.Admin);
        if (usersInAdminRole.Count > 0)
            return;

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            // User exists but has no Admin role yet — just promote them.
            var addResult = await userManager.AddToRoleAsync(existing, Roles.Admin);
            if (!addResult.Succeeded)
            {
                logger.LogError("Failed to promote {Email}: {Errors}", email, string.Join("; ", addResult.Errors.Select(e => e.Description)));
                return;
            }

            // Also ensure the base Student role is present so the admin can learn.
            var existingRoles = await userManager.GetRolesAsync(existing);
            if (!existingRoles.Contains(Roles.Student))
                await userManager.AddToRoleAsync(existing, Roles.Student);

            logger.LogInformation("Promoted existing user {Email} to Admin.", email);
            return;
        }

        var admin = new User(email, "System", "Administrator")
        {
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            logger.LogError("Failed to create seed admin {Email}: {Errors}", email,
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!roleResult.Succeeded)
        {
            logger.LogError("Admin user created but Admin role assignment failed: {Errors}",
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            return;
        }

        // Admin is also a Student — can learn on the platform.
        var studentResult = await userManager.AddToRoleAsync(admin, Roles.Student);
        if (studentResult.Succeeded)
            logger.LogInformation("Seeded admin account: {Email}", email);
        else
            logger.LogError("Admin user created but Student role assignment failed: {Errors}",
                string.Join("; ", studentResult.Errors.Select(e => e.Description)));
    }
}
