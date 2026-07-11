using Learnix.Application.Common.Abstractions.Storage;
using Learnix.DbMigrator.Constants;
using Learnix.DbMigrator.Seeders.Demo.CourseSeeders;
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
/// Opt-in dev seeder: creates a seed instructor and published courses across system categories.
/// Activate by setting SeedDevData:Enabled = true in appsettings.Development.json.
/// Idempotent — skips entirely if the seed instructor already owns any courses.
/// Each course contains PostLessons, VideoLessons, and TestLessons (all 3 types, ≥5 each).
/// Tests deliberately vary PassingThreshold and AttemptLimit for app-testing purposes.
/// </summary>
public sealed class CourseSeeder(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IOptions<BlobStorageOptions> blobOptions,
    ILogger<CourseSeeder> logger) : IDataSeeder
{
    /// <summary>
    /// Every seeded video is the same placeholder file, so its length is read once, from the header
    /// of the embedded asset itself — rather than hardcoding a number that silently goes stale the
    /// day the file is swapped.
    /// </summary>
    private static readonly Lazy<int?> PlaceholderVideoDuration = new(() =>
    {
        using var stream = typeof(CourseSeeder).Assembly
            .GetManifestResourceStream("Learnix.DbMigrator.Assets.placeholder.mp4");

        return stream is null ? null : Mp4Duration.TryRead(stream);
    });

    private static readonly SeedCourseDefinition[] SeedCourses =
    [
        CSharpFundamentalsSeeder.GetDefinition(),
        DesignPatternsSeeder.GetDefinition(),
        React19Seeder.GetDefinition(),
        PythonDataAnalysisSeeder.GetDefinition(),
        UiUxDesignSeeder.GetDefinition(),
        SqlDatabaseDesignSeeder.GetDefinition(),
        NodeJsRestApiSeeder.GetDefinition(),
        DigitalMarketingSeeder.GetDefinition(),
        AdvancedAlgorithmsSeeder.GetDefinition(),
        CloudArchitectureSeeder.GetDefinition(),
        HtmlCssBasicsSeeder.GetDefinition(),
        GitVersionControlSeeder.GetDefinition(),
        DockerForBeginnersSeeder.GetDefinition(),
        IntroToLinuxSeeder.GetDefinition(),
        AgileMethodologiesSeeder.GetDefinition()
    ];

    // S3776: a linear seed script — guard the config, ensure the instructor, then walk the course
    // definitions. It reads top to bottom; carving it into helpers would only scatter the order in
    // which demo data has to be created.
#pragma warning disable S3776
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration[$"{ConfigurationSectionNameConstants.SeedData}:InstructorEmail"];
        var password = configuration[$"{ConfigurationSectionNameConstants.SeedData}:InstructorPassword"];

        if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email, out _))
        {
            logger.LogWarning(
                "Course seeder: {Section}:InstructorEmail is missing or invalid — skipping course seeding.",
                ConfigurationSectionNameConstants.SeedData);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Course seeder: {Section}:InstructorPassword is not set — skipping course seeding.",
                ConfigurationSectionNameConstants.SeedData);
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();

        var instructor = await EnsureInstructorAsync(userManager, email, password);
        if (instructor is null)
            return;

        var alreadySeeded = await context.Courses
            .AnyAsync(c => c.InstructorId == instructor.Id, cancellationToken);

        if (!alreadySeeded)
        {
            var categoryIdBySlug = await context.Categories
                .Where(c => c.IsSystem)
                .ToDictionaryAsync(c => c.Slug, c => c.Id, cancellationToken);

            var seededCount = 0;
            foreach (var definition in SeedCourses)
            {
                if (!categoryIdBySlug.TryGetValue(definition.CategorySlug, out var categoryId))
                {
                    logger.LogWarning(
                        "Course seeder: category '{Slug}' not found — skipping '{Title}'.",
                        definition.CategorySlug, definition.Title);
                    continue;
                }

                await SeedSingleCourseAsync(
                    context, instructor.Id, categoryId, definition,
                    blobStorage, blobOptions, logger, cancellationToken);
                seededCount++;
            }

            logger.LogInformation(
                "Course seeder: seeded {Count} courses for instructor {Email}.", seededCount, email);
        }
        else
        {
            logger.LogInformation(
                "Course seeder: courses already exist for {Email} — skipping.", email);
        }

        var instructor2Email = "instructor2@learnix.dev";
        var instructor2 = await EnsureInstructorAsync(userManager, instructor2Email, password);
        if (instructor2 != null)
        {
            var alreadySeeded2 = await context.Courses
                .AnyAsync(c => c.InstructorId == instructor2.Id, cancellationToken);

            if (!alreadySeeded2)
            {
                var categoryIdBySlug = await context.Categories
                    .Where(c => c.IsSystem)
                    .ToDictionaryAsync(c => c.Slug, c => c.Id, cancellationToken);

                var categorySlugs = new[] { "programming", "web-development", "data-science", "design", "business", "marketing", "personal-development", "language-learning" };

                // S2245: this only picks a category for a throwaway demo course — nothing here is a
                // secret or a security decision, so a PRNG is the right tool.
#pragma warning disable S2245
                var random = new Random();
#pragma warning restore S2245

                for (int i = 1; i <= 10; i++)
                {
                    var catSlug = categorySlugs[random.Next(categorySlugs.Length)];
                    if (!categoryIdBySlug.TryGetValue(catSlug, out var catId)) continue;

                    var def = new SeedCourseDefinition(
                        catSlug,
                        $"Generic Test Course {i}",
                        "This is a generic course created for testing pagination and display.",
                        random.NextDouble() > 0.5 ? 0m : 19.99m,
                        ["generic", "test"],
                        [
                            new SeedSection("Section 1", [
                                new SeedVideo("Video Lesson 1", "Generic video lesson content."),
                                new SeedPost("Post Lesson 2", "Generic post lesson content."),
                                new SeedPost("Post Lesson 3", "Generic post lesson content.")
                            ])
                        ],
                        "generic_thumbnail.webp"
                    );

                    await SeedSingleCourseAsync(
                        context, instructor2.Id, catId, def,
                        blobStorage, blobOptions, logger, cancellationToken);
                }

                logger.LogInformation(
                    "Course seeder: seeded 10 generic courses for instructor {Email}.", instructor2Email);
            }
            else
            {
                logger.LogInformation(
                    "Course seeder: courses already exist for {Email} — skipping.", instructor2Email);
            }
        }
    }
#pragma warning restore S3776




    private async Task<User?> EnsureInstructorAsync(
        UserManager<User> userManager,
        string email,
        string password)
    {
        var instructor = await userManager.FindByEmailAsync(email);

        if (instructor is null)
        {
            instructor = new User(email, "Dev", "Instructor") { EmailConfirmed = true };
            var result = await userManager.CreateAsync(instructor, password);

            if (!result.Succeeded)
            {
                logger.LogError(
                    "Dev seeder: failed to create instructor {Email}: {Errors}",
                    email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return null;
            }

            logger.LogInformation("Dev seeder: created instructor account {Email}.", email);
        }

        if (!await userManager.IsInRoleAsync(instructor, Roles.Instructor))
            await userManager.AddToRoleAsync(instructor, Roles.Instructor);

        if (!await userManager.IsInRoleAsync(instructor, Roles.Student))
            await userManager.AddToRoleAsync(instructor, Roles.Student);

        return instructor;
    }

    // S107/S3776: seeding one course means creating the course, its sections, its lessons of three
    // different kinds and its cover blob, in that order. Every parameter is a collaborator it needs,
    // and the branches are the lesson kinds.
#pragma warning disable S107, S3776
    private static async Task SeedSingleCourseAsync(
        ApplicationDbContext context,
        Guid instructorId,
        Guid categoryId,
        SeedCourseDefinition definition,
        IBlobStorageService blobStorage,
        IOptions<BlobStorageOptions> blobOptions,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Step 1 — persist course + sections so their IDs are stable before adding lessons.
        var course = Course.Create(
            instructorId, categoryId,
            definition.Title, definition.Description,
            definition.Price, definition.Tags);

        var coverPath = $"{blobOptions.Value.CourseCoverContainer}/{Guid.NewGuid()}-cover.webp";
        try
        {
            var assembly = typeof(CourseSeeder).Assembly;
            using var stream = assembly.GetManifestResourceStream($"Learnix.DbMigrator.Assets.{definition.ImageName}");
            if (stream != null)
            {
                await blobStorage.UploadAsync(coverPath, stream, "image/webp", cancellationToken);
                course.SetCoverImage(coverPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to upload course cover for {Title}", definition.Title);
        }

        context.Courses.Add(course);

        var sections = definition.Sections
            .Select(s => (Entity: course.AddSection(s.Title), Def: s))
            .ToList();

        await context.SaveChangesAsync(cancellationToken);

        // Step 2 — add lessons through the aggregate root, which assigns DisplayOrder from the
        // section and so keeps the unique (SectionId, DisplayOrder) index satisfied.
        //
        // Notes on visibility:
        //   PostLesson.Create()  в†’ sets IsHidden = false automatically when content is non-empty.
        //   VideoLesson.Create() в†’ IsHidden stays true (base default); must call SetVisibility(false).
        //   TestLesson.Create()  в†’ IsHidden stays true even after ReplaceQuestions(); must call SetVisibility(false).
        foreach (var (section, sectionDef) in sections)
        {
            foreach (var lessonDef in sectionDef.Lessons)
            {
                switch (lessonDef)
                {
                    case SeedPost post:
                        course.AddLesson(PostLesson.Create(section.Id, post.Title, post.Content));
                        break;

                    case SeedVideo vid:
                        var videoPath = $"{blobOptions.Value.LessonVideoContainer}/{Guid.NewGuid()}-placeholder.mp4";
                        try
                        {
                            var assembly = typeof(CourseSeeder).Assembly;
                            using var stream = assembly.GetManifestResourceStream("Learnix.DbMigrator.Assets.placeholder.mp4");
                            if (stream != null)
                            {
                                await blobStorage.UploadAsync(videoPath, stream, "video/mp4", cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to upload placeholder video for {Title}", vid.Title);
                        }

                        course.AddLesson(VideoLesson.Create(
                            section.Id, vid.Title, videoPath, vid.Description, PlaceholderVideoDuration.Value));
                        break;

                    case SeedTest test:
                        var tl = TestLesson.Create(
                            section.Id, test.Title,
                            test.Description, test.AttemptLimit,
                            test.CooldownMinutes, test.PassingThreshold);
                        tl.ReplaceQuestions(test.Questions);
                        course.AddLesson(tl);
                        break;
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        // Step 3 — reload with full navigation so Publish() can validate the in-memory
        // lesson collection. Section._lessons is a backing field populated by EF on load.
        var fullCourse = await context.Courses
            .Include(c => c.Sections)
            .ThenInclude(s => s.Lessons)
            .FirstAsync(c => c.Id == course.Id, cancellationToken);

        // Toggle visibility via the aggregate root
        foreach (var sec in fullCourse.Sections)
        {
            foreach (var les in sec.Lessons)
            {
                if (les.IsHidden)
                {
                    fullCourse.ToggleLessonVisibility(les, isVisible: true);
                }
            }
        }

        fullCourse.Publish();

        await context.SaveChangesAsync(cancellationToken);
    }
#pragma warning restore S107, S3776
}



