using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence;
using Learnix.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnix.Infrastructure.Services;

/// <summary>
/// Opt-in dev seeder: creates a seed student account with all achievements unlocked.
/// Activate via SeedDevData:Enabled = true in appsettings.Development.json.
/// Requires SeedDevData:StudentEmail and SeedDevData:StudentPassword to be set.
/// Idempotent — skips if the student already has any UserAchievement rows.
/// Domain events on created entities are cleared before SaveChanges so no
/// outbox noise is generated during seeding.
/// </summary>
public sealed class DevStudentSeederHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostEnvironment environment,
    IOptions<BlobStorageOptions> blobOptions,
    ILogger<DevStudentSeederHostedService> logger) : IHostedService
{
    // Reuse the same 100×100 blue→white gradient PNG as the course seeder.
    private static readonly byte[] PlaceholderPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAGQAAABkCAYAAABw4pVUAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8" +
        "YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAI7SURBVHhe7dGLao4BAIBh9yVJkiRJkiRJkkSSJEnSMGuG" +
        "WTMMs2aYNcMwa4ZZM8zaMMtx1gxzWjPs/87fq1yG3ucWnhkziwrM2ltg9v4Cc4oLzD1QYF5JgfmlAQsO" +
        "BiwsC1h0KGDxkYAl5QFLjwYsqwhYXhmw4ljAyqqAVcdDVp8MWVMdsvZUyLozIetrQjacDdlYG7KpLmTz" +
        "uZAt9SFbz0dsuxixvSFix6WInY0Ru5oidl+O2NMcUXQlYt+1iOKWiJLrMaU3Y8paYw7fiilvi6loj6m8" +
        "HVPVEXPiTkz1vZjTnTE19xNquxLquhPqHyRc6EloeJjQ+DihqTeh+UnC1b6Elv6EGwMprU9T2p6ntA+m" +
        "dLxIuTuU0vkypetVSvfrlJ43KY/epfQOZ/S9z+gfyRgYzXj2IWNwLGPoY8bLzxmvxzPefskY/pYx8j1n" +
        "9EfO2ETOp8mc8Z85X6dyvv/KmfiTMzmdM1XI+R3kTEcQxBAlkKSQ5fxjiCGGGGKIIYYYYoghhhhiiCGG" +
        "GGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGG" +
        "GGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGG" +
        "GGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYsh/HgJ/ATQ2" +
        "fg8xyAy9AAAAAElFTkSuQmCC");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return;

        if (!string.Equals(
                configuration["SeedDevData:Enabled"], "true",
                StringComparison.OrdinalIgnoreCase))
            return;

        var email = configuration["SeedDevData:StudentEmail"] ?? "student@learnix.dev";
        var password = configuration["SeedDevData:StudentPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Dev student seeder: SeedDevData:StudentPassword is not set — skipping.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();

        var student = await EnsureStudentAsync(userManager, email, password);
        if (student is null)
            return;

        var alreadySeeded = await db.Set<UserAchievement>()
            .AnyAsync(a => a.UserId == student.Id, cancellationToken);

        if (alreadySeeded)
        {
            logger.LogInformation(
                "Dev student seeder: achievements already exist for {Email} — skipping.", email);
            return;
        }

        // ── Avatar (best-effort) ──────────────────────────────────────────
        var avatarPath = $"{blobOptions.Value.AvatarContainer}/seed-student-avatar.png";
        try
        {
            await blobStorage.UploadAsync(
                avatarPath, new MemoryStream(PlaceholderPng), "image/png", cancellationToken);
            await blobStorage.MarkConfirmedAsync(avatarPath, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Dev student seeder: could not upload avatar placeholder — " +
                "is blob storage running? Proceeding without avatar.");
            avatarPath = string.Empty;
        }

        // ── Profile ───────────────────────────────────────────────────────
        student.UpdateProfile(
            "Dev", "Student",
            "A fully-seeded development student account with all achievements unlocked.");

        if (!string.IsNullOrEmpty(avatarPath))
            student.SetAvatar(avatarPath);

        student.ClearDomainEvents();

        // ── Progress counters ─────────────────────────────────────────────
        var progress = UserAchievementProgress.Create(student.Id);
        progress.SetLessonsCompleted(500);
        progress.SetCoursesCompleted(5);
        progress.SetDistinctCategoriesCompleted(3);
        progress.SetProfileCompleted(!string.IsNullOrEmpty(avatarPath));
        db.Set<UserAchievementProgress>().Add(progress);

        // ── Completed categories (first 3 system categories, alphabetically) ──
        var categoryIds = await db.Categories
            .Where(c => c.IsSystem)
            .OrderBy(c => c.Name)
            .Select(c => c.Id)
            .Take(AchievementCodes.PolymathMinCategories)
            .ToListAsync(cancellationToken);

        foreach (var categoryId in categoryIds)
            db.Set<UserCompletedCategory>().Add(
                UserCompletedCategory.Create(student.Id, categoryId));

        // ── Achievements ──────────────────────────────────────────────────
        // Use Unlock() for correct entity construction, then immediately clear
        // domain events so no outbox messages are written during seeding.
        foreach (var code in AchievementCodes.All)
        {
            var achievement = UserAchievement.Unlock(student.Id, code);
            achievement.MarkSeen();
            achievement.ClearDomainEvents();
            db.Set<UserAchievement>().Add(achievement);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Dev student seeder: seeded {Email} with {Count} achievements.",
            email, AchievementCodes.All.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<User?> EnsureStudentAsync(
        UserManager<User> userManager,
        string email,
        string password)
    {
        var student = await userManager.FindByEmailAsync(email);

        if (student is null)
        {
            student = new User(email, "Dev", "Student") { EmailConfirmed = true };
            var result = await userManager.CreateAsync(student, password);

            if (!result.Succeeded)
            {
                logger.LogError(
                    "Dev student seeder: failed to create student {Email}: {Errors}",
                    email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return null;
            }

            logger.LogInformation("Dev student seeder: created student account {Email}.", email);
        }

        // Guard in case this account was previously promoted to another role.
        if (await userManager.IsInRoleAsync(student, Roles.Admin)
            || await userManager.IsInRoleAsync(student, Roles.Instructor))
        {
            logger.LogWarning(
                "Dev student seeder: {Email} has elevated roles — skipping achievement seeding " +
                "to avoid cross-contaminating a non-student account.", email);
            return null;
        }

        if (!await userManager.IsInRoleAsync(student, Roles.Student))
            await userManager.AddToRoleAsync(student, Roles.Student);

        return student;
    }
}
