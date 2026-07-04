using Learnix.Application.Common.Abstractions.Storage;
using Learnix.DbMigrator.Constants;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Learnix.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnix.DbMigrator.Seeders;

/// <summary>
/// Opt-in dev seeder: creates a seed student account with all achievements unlocked.
/// Activate via SeedDevData:Enabled = true in appsettings.Development.json.
/// Requires SeedDevData:StudentEmail and SeedDevData:StudentPassword to be set.
/// Idempotent — skips if the student already has any UserAchievement rows.
/// Domain events on created entities are cleared before SaveChanges so no
/// outbox noise is generated during seeding.
/// </summary>
public sealed class StudentSeeder(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IOptions<BlobStorageOptions> blobOptions,
    ILogger<StudentSeeder> logger) : IDataSeeder
{

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration[$"{ConfigurationSectionNameConstants.SeedData}:StudentEmail"];
        var password = configuration[$"{ConfigurationSectionNameConstants.SeedData}:StudentPassword"];

        if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email, out _))
        {
            logger.LogWarning($"Student seeder: {ConfigurationSectionNameConstants.SeedData}:StudentEmail is missing or invalid — skipping.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning($"Student seeder: {ConfigurationSectionNameConstants.SeedData}:StudentPassword is not set — skipping.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();

        var student = await EnsureStudentAsync(userManager, email, password);
        if (student is null)
            return;

        for (int i = 1; i <= 15; i++)
        {
            var dummyEmail = $"learnix-student-dev-{i}@learnix.dev";
            await EnsureStudentAsync(userManager, dummyEmail, password, $"Student_{i}");
        }

        var courses = await db.Courses.ToListAsync(cancellationToken);
        if (courses.Count > 0)
        {
            var random = new Random();
            var dummyStudents = new List<User>();

            for (int i = 1; i <= 15; i++)
            {
                var dummyEmail = $"learnix-student-dev-{i}@learnix.dev";
                var dummyUser = await userManager.FindByEmailAsync(dummyEmail);
                if (dummyUser is not null) dummyStudents.Add(dummyUser);
            }

            foreach (var course in courses)
            {
                int numReviews = random.Next(4, 9); // 4 to 8 reviews
                var selectedStudents = dummyStudents.OrderBy(x => random.Next()).Take(numReviews).ToList();

                foreach (var dummyUser in selectedStudents)
                {
                    var exists = await db.Set<Enrollment>().AnyAsync(e => e.CourseId == course.Id && e.StudentId == dummyUser.Id, cancellationToken);
                    if (!exists)
                    {
                        var enrollment = Enrollment.Create(course.Id, dummyUser.Id, 0m);
                        db.Set<Enrollment>().Add(enrollment);
                        course.IncrementEnrollmentsCount();

                        var reviewExists = await db.Set<CourseReview>().AnyAsync(r => r.CourseId == course.Id && r.StudentId == dummyUser.Id, cancellationToken);
                        if (!reviewExists)
                        {
                            int rating = random.Next(3, 6); // 3, 4 or 5
                            string[] reviews = [
                                "Great course!",
                                "I really enjoyed this course. The materials were very clear and well organized.",
                                "This course completely exceeded my expectations. The instructor explained the complex topics in a very easy-to-understand manner, and the practical exercises were extremely helpful for solidifying my knowledge. Highly recommended to anyone looking to master this subject!",
                                "Very informative and engaging. Would recommend.",
                                "Good content but could be a bit slower in pace.",
                                "Excellent structure and practical examples."
                            ];
                            string comment = reviews[random.Next(reviews.Length)];
                            var review = CourseReview.Create(course.Id, dummyUser.Id, rating, comment);
                            db.Set<CourseReview>().Add(review);
                        }
                    }
                }
            }
            await db.SaveChangesAsync(cancellationToken);

            // Re-sync ratings from DB for accuracy
            foreach (var course in courses)
            {
                var stats = await db.Set<CourseReview>()
                    .Where(r => r.CourseId == course.Id)
                    .GroupBy(r => r.CourseId)
                    .Select(g => new { Count = g.Count(), Average = g.Average(r => (decimal)r.Rating) })
                    .FirstOrDefaultAsync(cancellationToken);

                if (stats != null)
                {
                    course.SyncRating(stats.Count, Math.Round(stats.Average, 2));
                }
            }
            await db.SaveChangesAsync(cancellationToken);
        }


        var alreadySeeded = await db.Set<UserAchievement>()
            .AnyAsync(a => a.UserId == student.Id, cancellationToken);

        if (alreadySeeded)
        {
            logger.LogInformation(
                "Student seeder: achievements already exist for {Email} — skipping.", email);
            return;
        }

        // Avatar (best-effort) 
        var avatarPath = $"{blobOptions.Value.AvatarContainer}/{Guid.NewGuid()}-student-avatar.png";
        try
        {
            var assembly = typeof(StudentSeeder).Assembly;
            using var stream = assembly.GetManifestResourceStream("Learnix.DbMigrator.Assets.generic_thumbnail.png");

            if (stream != null)
            {
                await blobStorage.UploadAsync(avatarPath, stream, "image/png", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Student seeder: could not upload avatar placeholder — " +
                "is blob storage running? Proceeding without avatar.");
            avatarPath = string.Empty;
        }

        // Profile 
        student.UpdateProfile(
            "Dev", "Student",
            "A fully-seeded development student account with all achievements unlocked.");

        if (!string.IsNullOrEmpty(avatarPath))
            student.SetAvatar(avatarPath);

        student.ClearDomainEvents();

        // Progress counters 
        var progress = UserAchievementProgress.Create(student.Id);
        progress.SetLessonsCompleted(500);
        progress.SetCoursesCompleted(5);
        progress.SetDistinctCategoriesCompleted(3);
        progress.SetProfileCompleted(!string.IsNullOrEmpty(avatarPath));
        db.Set<UserAchievementProgress>().Add(progress);

        // Completed categories (first 3 system categories, alphabetically) 
        var categoryIds = await db.Categories
            .Where(c => c.IsSystem)
            .OrderBy(c => c.Name)
            .Select(c => c.Id)
            .Take(AchievementCodes.PolymathMinCategories)
            .ToListAsync(cancellationToken);

        foreach (var categoryId in categoryIds)
            db.Set<UserCompletedCategory>().Add(
                UserCompletedCategory.Create(student.Id, categoryId));

        // Achievements 
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
            "Student seeder: seeded {Email} with {Count} achievements.",
            email, AchievementCodes.All.Length);
    }



    private async Task<User?> EnsureStudentAsync(
        UserManager<User> userManager,
        string email,
        string password,
        string lastName = "Student")
    {
        var student = await userManager.FindByEmailAsync(email);

        if (student is null)
        {
            student = new User(email, "Dev", lastName) { EmailConfirmed = true };
            var result = await userManager.CreateAsync(student, password);

            if (!result.Succeeded)
            {
                logger.LogError(
                    "Student seeder: failed to create student {Email}: {Errors}",
                    email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return null;
            }

            logger.LogInformation("Student seeder: created student account {Email}.", email);
        }

        // Guard in case this account was previously promoted to another role.
        if (await userManager.IsInRoleAsync(student, Roles.Admin)
            || await userManager.IsInRoleAsync(student, Roles.Instructor))
        {
            logger.LogWarning(
                "Student seeder: {Email} has elevated roles — skipping achievement seeding " +
                "to avoid cross-contaminating a non-student account.", email);
            return null;
        }

        if (!await userManager.IsInRoleAsync(student, Roles.Student))
            await userManager.AddToRoleAsync(student, Roles.Student);

        return student;
    }
}
