using Learnix.Application.Common.Abstractions.Storage;
using Learnix.DbMigrator.Constants;
using Learnix.Domain.Common;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Learnix.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    ILogger<StudentSeeder> logger) : IDataSeeder
{
    // S2245: this randomness only picks demo reviewers and demo ratings — nothing here is a secret,
    // a token, or a decision an attacker could exploit, so a PRNG is the right tool.
#pragma warning disable S2245
    private static readonly Random Rng = new();
#pragma warning restore S2245

    private const int DummyStudentCount = 25;

    /// <summary>Demo activity (enrollments, payments, progress, reviews) is spread across this window.</summary>
    private const int SeedWindowDays = 35;

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

        var dummyStudents = new List<User>(DummyStudentCount);
        for (int i = 1; i <= DummyStudentCount; i++)
        {
            var dummyEmail = $"learnix-student-dev-{i}@learnix.dev";
            var dummyStudent = await EnsureStudentAsync(userManager, dummyEmail, password, $"Student_{i}");
            if (dummyStudent is not null)
            {
                dummyStudents.Add(dummyStudent);
            }
        }

        var courses = await db.Courses
            .Include(c => c.Sections)
            .ThenInclude(s => s.Lessons)
            .ToListAsync(cancellationToken);
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
        var avatarPath = $"{BlobContainers.Avatars}/{Guid.NewGuid()}-student-avatar.webp";

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
    /// Enrolls a random subset of the dummy students into every course and, for each, seeds a payment
    /// (paid courses), lesson-by-lesson progress, a possible completion + certificate, and a review —
    /// all dated across the last <see cref="SeedWindowDays"/> days so the instructor analytics have a
    /// real time series, funnel, drop-off curve and active-student count to draw.
    /// Idempotent: a course/student pair that already has an enrollment is skipped whole.
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

        var now = DateTime.UtcNow;

        // Rows whose CreatedAt drives a time-series (payment revenue, review trend). The auditable
        // interceptor forces CreatedAt = now on insert, so the intended date is recorded here and
        // rewritten in a second pass, where the interceptor only touches UpdatedAt.
        var createdAtBackfill = new List<(object Entity, DateTime CreatedAt)>();

        foreach (var course in courses)
        {
            var lessons = course.Sections
                .OrderBy(s => s.DisplayOrder)
                .SelectMany(s => s.Lessons.Where(l => !l.IsHidden).OrderBy(l => l.DisplayOrder))
                .ToList();
            var totalLessons = lessons.Count;

            // Generic courses only pad pagination — keep their popularity low so they don't outrank
            // the real courses in the Featured section.
            var isGeneric = course.Tags.Contains("generic");
            var enrollerCount = isGeneric ? Rng.Next(2, 5) : Rng.Next(9, 16);

            var enrollers = dummyStudents
                .OrderBy(_ => Rng.Next())
                .Take(Math.Min(enrollerCount, dummyStudents.Count))
                .ToList();

            foreach (var studentUser in enrollers)
            {
                var studentId = studentUser.Id;
                var key = $"{course.Id}_{studentId}";
                if (!existingEnrollmentSet.Add(key))
                    continue;

                var enrolledAt = RandomPastInstant(now, SeedWindowDays);

                var enrollment = Enrollment.Create(course.Id, studentId, course.Price);
                if (course.Price > 0m)
                    enrollment.ConfirmPayment();

                db.Set<Enrollment>().Add(enrollment);
                SetTracked(db, enrollment, nameof(Enrollment.EnrolledAt), enrolledAt);
                course.IncrementEnrollmentsCount();

                if (course.Price > 0m)
                {
                    var payment = Payment.CreateMock(studentId, course.Id, enrollment.Id, course.Price);
                    db.Set<Payment>().Add(payment);
                    SetTracked(db, payment, nameof(Payment.CompletedAt), enrolledAt);
                    createdAtBackfill.Add((payment, enrolledAt));
                }

                // Complete the first N lessons in order. N follows a distribution that leaves most
                // students partway through, so the drop-off curve descends and the funnel narrows.
                var completedCount = PickCompletedCount(totalLessons);
                var lastActivity = enrolledAt;

                for (var i = 0; i < completedCount; i++)
                {
                    var progress = LessonProgress.Create(course.Id, lessons[i].Id, studentId);
                    progress.MarkCompleted();
                    progress.ClearDomainEvents();

                    var completedAt = Lerp(enrolledAt, now, (double)(i + 1) / (totalLessons + 1));
                    db.Set<LessonProgress>().Add(progress);
                    SetTracked(db, progress, nameof(LessonProgress.CompletedAt), completedAt);
                    SetTracked(db, progress, nameof(LessonProgress.LastAccessedAt), completedAt);
                    lastActivity = completedAt;
                }

                if (totalLessons > 0 && completedCount == totalLessons)
                {
                    enrollment.MarkCompleted();
                    enrollment.ClearDomainEvents();
                    SetTracked(db, enrollment, nameof(Enrollment.CompletedAt), lastActivity);

                    var certificate = Certificate.Issue(enrollment, course);
                    certificate.ClearDomainEvents();
                    db.Set<Certificate>().Add(certificate);
                    SetTracked(db, certificate, nameof(Certificate.IssuedAt), lastActivity);
                }

                // Reviews come only from students who actually started (matches the review gate).
                if (completedCount >= 1 && Rng.NextDouble() < 0.7)
                {
                    var review = CourseReview.Create(
                        course.Id,
                        studentId,
                        PickRating(),
                        ReviewComments[Rng.Next(ReviewComments.Length)]);
                    review.CaptureProgress(completedCount, totalLessons);
                    db.Set<CourseReview>().Add(review);

                    createdAtBackfill.Add((review, Lerp(lastActivity, now, Rng.NextDouble())));
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var (entity, createdAt) in createdAtBackfill)
            db.Entry(entity).Property(nameof(IAuditable.CreatedAt)).CurrentValue = createdAt;

        if (createdAtBackfill.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }

    private static void SetTracked(ApplicationDbContext db, object entity, string property, DateTime value)
        => db.Entry(entity).Property(property).CurrentValue = value;

    /// <summary>A random instant within the last <paramref name="windowDays"/> days.</summary>
    private static DateTime RandomPastInstant(DateTime now, int windowDays)
        => now.AddDays(-Rng.Next(0, windowDays))
              .AddHours(-Rng.Next(0, 24))
              .AddMinutes(-Rng.Next(0, 60));

    /// <summary>Linear interpolation between two instants (t clamped to 0..1).</summary>
    private static DateTime Lerp(DateTime from, DateTime to, double t)
    {
        if (to <= from)
            return from;

        return from.AddSeconds((to - from).TotalSeconds * Math.Clamp(t, 0, 1));
    }

    /// <summary>
    /// How many lessons a student completed: ~15% never start, ~60% get partway, ~25% finish — a
    /// shape that gives the funnel and per-lesson drop-off something to show.
    /// </summary>
    private static int PickCompletedCount(int totalLessons)
    {
        if (totalLessons == 0)
            return 0;

        var roll = Rng.NextDouble();
        if (roll < 0.15)
            return 0;
        if (roll < 0.75)
            return Math.Clamp(
                (int)Math.Round(totalLessons * (0.2 + (Rng.NextDouble() * 0.7))), 1, totalLessons);

        return totalLessons;
    }

    /// <summary>Ratings skewed high with a long tail, so the distribution chart is not a flat block.</summary>
    private static int PickRating()
    {
        var r = Rng.NextDouble();
        if (r < 0.5)
            return 5;
        if (r < 0.75)
            return 4;
        if (r < 0.9)
            return 3;
        if (r < 0.97)
            return 2;
        return 1;
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
