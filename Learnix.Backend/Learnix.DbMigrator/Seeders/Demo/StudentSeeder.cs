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
/// Requires SeedData:StudentEmail and SeedData:StudentPassword to be set.
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
    // S2245: this randomness only picks demo reviewers and demo ratings — nothing here is a secret,
    // a token, or a decision an attacker could exploit, so a PRNG is the right tool.
#pragma warning disable S2245
    private static readonly Random Rng = new();
#pragma warning restore S2245

    private static readonly string[] ReviewComments =
    [
        "Great course!",
        "I really enjoyed this course. The materials were very clear and well organized.",
        "This course completely exceeded my expectations. The instructor explained the complex topics in a very easy-to-understand manner, and the practical exercises were extremely helpful for solidifying my knowledge. Highly recommended to anyone looking to master this subject!",
        "Very informative and engaging. Would recommend.",
        "Good content but could be a bit slower in pace.",
        "Excellent structure and practical examples.",
        "I've taken many online courses over the years, but this one stands out as a true masterpiece of educational design. From the very first lesson, it was clear that an immense amount of thought went into structuring the curriculum. The progression from fundamental concepts to advanced architecture is seamless, ensuring that you are never left behind but constantly challenged. The instructor doesn't just read from slides; they share battle-tested wisdom from real-world production environments, highlighting common pitfalls and edge cases that you would typically only learn through painful experience. The assignments are perfectly calibrated to reinforce the material without feeling like busywork, and the quality of the video and audio production is top-notch. Whether you are an absolute beginner looking for a solid foundation or a seasoned developer aiming to plug gaps in your knowledge, this course is an absolute must-have. I cannot recommend it highly enough, and I will definitely be returning to these materials as a reference throughout my career!"
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration[$"{ConfigurationSectionNameConstants.SeedData}:StudentEmail"];
        var password = configuration[$"{ConfigurationSectionNameConstants.SeedData}:StudentPassword"];

        if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email, out _))
        {
            logger.LogWarning(
                "Student seeder: {Section}:StudentEmail is missing or invalid — skipping.",
                ConfigurationSectionNameConstants.SeedData);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Student seeder: {Section}:StudentPassword is not set — skipping.",
                ConfigurationSectionNameConstants.SeedData);
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();

        var student = await EnsureStudentAsync(userManager, email, password);
        if (student is null)
            return;

        var dummyStudents = new List<User>(15);
        for (int i = 1; i <= 15; i++)
        {
            var dummyEmail = $"learnix-student-dev-{i}@learnix.dev";
            var dummyStudent = await EnsureStudentAsync(userManager, dummyEmail, password, $"Student_{i}");
            if (dummyStudent is not null)
            {
                dummyStudents.Add(dummyStudent);
            }
        }

        var courses = await db.Courses.ToListAsync(cancellationToken);
        if (courses.Count > 0 && dummyStudents.Count > 0)
        {
            await SeedEnrollmentsAndReviewsAsync(db, courses, dummyStudents, cancellationToken);
            await SyncCourseRatingsAsync(db, courses, cancellationToken);
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
        var avatarPath = await UploadAvatarAsync(blobStorage, cancellationToken);

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



    private async Task<string> UploadAvatarAsync(IBlobStorageService blobStorage, CancellationToken cancellationToken)
    {
        var avatarPath = $"{blobOptions.Value.AvatarContainer}/{Guid.NewGuid()}-student-avatar.webp";

        try
        {
            var assembly = typeof(StudentSeeder).Assembly;
            using var stream = assembly.GetManifestResourceStream("Learnix.DbMigrator.Assets.generic_thumbnail.webp");

            if (stream is not null)
                await blobStorage.UploadAsync(avatarPath, stream, "image/webp", cancellationToken);

            return avatarPath;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Student seeder: could not upload avatar placeholder — " +
                "is blob storage running? Proceeding without avatar.");
            return string.Empty;
        }
    }

    /// <summary>
    /// Enrolls a random subset of the dummy students into every course and leaves a review from each.
    /// Idempotent: existing (course, student) enrollments and reviews are skipped.
    /// </summary>
    private static async Task SeedEnrollmentsAndReviewsAsync(
        ApplicationDbContext db,
        List<Course> courses,
        List<User> dummyStudents,
        CancellationToken cancellationToken)
    {
        var dummyStudentIds = dummyStudents.Select(u => u.Id).ToList();

        var existingEnrollmentSet = (await db.Set<Enrollment>()
            .Where(e => dummyStudentIds.Contains(e.StudentId))
            .Select(e => new { e.CourseId, e.StudentId })
            .ToListAsync(cancellationToken))
            .Select(e => $"{e.CourseId}_{e.StudentId}")
            .ToHashSet();

        var existingReviewSet = (await db.Set<CourseReview>()
            .Where(r => dummyStudentIds.Contains(r.StudentId))
            .Select(r => new { r.CourseId, r.StudentId })
            .ToListAsync(cancellationToken))
            .Select(r => $"{r.CourseId}_{r.StudentId}")
            .ToHashSet();

        foreach (var course in courses)
        {
            // Generic courses exist only to pad pagination, so keep their popularity low: a handful
            // of reviews (~1-2) instead of the fuller set (~6) real courses get. This stops random
            // filler courses from outranking the real ones in the popularity-based Featured section.
            var isGeneric = course.Tags.Contains("generic");
            var reviewerCount = isGeneric ? Rng.Next(1, 3) : Rng.Next(5, 8);

            var reviewerIds = dummyStudents
                .OrderBy(_ => Rng.Next())
                .Take(reviewerCount)
                .Select(reviewer => reviewer.Id)
                .ToList();

            foreach (var reviewerId in reviewerIds)
            {
                var key = $"{course.Id}_{reviewerId}";

                if (!existingEnrollmentSet.Add(key))
                    continue;

                db.Set<Enrollment>().Add(Enrollment.Create(course.Id, reviewerId, 0m));
                course.IncrementEnrollmentsCount();

                if (existingReviewSet.Add(key))
                {
                    db.Set<CourseReview>().Add(CourseReview.Create(
                        course.Id,
                        reviewerId,
                        Rng.Next(3, 6),
                        ReviewComments[Rng.Next(ReviewComments.Length)]));
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Recomputes each course's rating from the reviews actually stored, so the counters match the rows.</summary>
    private static async Task SyncCourseRatingsAsync(
        ApplicationDbContext db,
        List<Course> courses,
        CancellationToken cancellationToken)
    {
        var courseIds = courses.Select(c => c.Id).ToList();

        var stats = await db.Set<CourseReview>()
            .Where(r => courseIds.Contains(r.CourseId))
            .GroupBy(r => r.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count(), Average = g.Average(r => (decimal)r.Rating) })
            .ToDictionaryAsync(x => x.CourseId, cancellationToken);

        foreach (var course in courses)
        {
            if (stats.TryGetValue(course.Id, out var courseStats))
                course.SyncRating(courseStats.Count, Math.Round(courseStats.Average, 2));
        }

        await db.SaveChangesAsync(cancellationToken);
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
