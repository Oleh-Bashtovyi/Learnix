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

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration[$"{ConfigurationSectionNameConstants.SeedData}:InstructorEmail"];
        var password = configuration[$"{ConfigurationSectionNameConstants.SeedData}:InstructorPassword"];

        if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email, out _))
        {
            logger.LogWarning($"Course seeder: {ConfigurationSectionNameConstants.SeedData}:InstructorEmail is missing or invalid — skipping course seeding.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning($"Course seeder: {ConfigurationSectionNameConstants.SeedData}:InstructorPassword is not set — skipping course seeding.");
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
                var random = new Random();

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
                        "generic_thumbnail.png"
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

    private static async Task SeedSingleCourseAsync(
        ApplicationDbContext context,
        Guid instructorId,
        Guid categoryId,
        SeedCourseDefinition definition,
        IBlobStorageService blobStorage,
        IOptions<BlobStorageOptions> blobOptions,
        ILogger logger,
        CancellationToken ct)
    {
        // Step 1 — persist course + sections so their IDs are stable before adding lessons.
        var course = Course.Create(
            instructorId, categoryId,
            definition.Title, definition.Description,
            definition.Price, definition.Tags);

        var coverPath = $"{blobOptions.Value.CourseCoverContainer}/{Guid.NewGuid()}-cover.png";
        try
        {
            var assembly = typeof(CourseSeeder).Assembly;
            using var stream = assembly.GetManifestResourceStream($"Learnix.DbMigrator.Assets.{definition.ImageName}");
            if (stream != null)
            {
                await blobStorage.UploadAsync(coverPath, stream, "image/png", ct);
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

        await context.SaveChangesAsync(ct);

        // Step 2 — add lessons via DbContext (Section.AddLesson is internal to the Domain assembly).
        // The unique (SectionId, DisplayOrder) index requires stable order values.
        //
        // Notes on visibility:
        //   PostLesson.Create()  в†’ sets IsHidden = false automatically when content is non-empty.
        //   VideoLesson.Create() в†’ IsHidden stays true (base default); must call SetVisibility(false).
        //   TestLesson.Create()  в†’ IsHidden stays true even after ReplaceQuestions(); must call SetVisibility(false).
        foreach (var (section, sectionDef) in sections)
        {
            for (var order = 0; order < sectionDef.Lessons.Length; order++)
            {
                switch (sectionDef.Lessons[order])
                {
                    case SeedPost post:
                        context.Set<PostLesson>().Add(
                            PostLesson.Create(section.Id, post.Title, order, post.Content));
                        break;

                    case SeedVideo vid:
                        var videoPath = $"{blobOptions.Value.LessonVideoContainer}/{Guid.NewGuid()}-placeholder.mp4";
                        try
                        {
                            var assembly = typeof(CourseSeeder).Assembly;
                            using var stream = assembly.GetManifestResourceStream("Learnix.DbMigrator.Assets.placeholder.mp4");
                            if (stream != null)
                            {
                                await blobStorage.UploadAsync(videoPath, stream, "video/mp4", ct);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to upload placeholder video for {Title}", vid.Title);
                        }

                        var vl = VideoLesson.Create(
                            section.Id, vid.Title, order, videoPath, vid.Description);
                        context.Set<VideoLesson>().Add(vl);
                        break;

                    case SeedTest test:
                        var tl = TestLesson.Create(
                            section.Id, test.Title, order,
                            test.Description, test.AttemptLimit,
                            test.CooldownMinutes, test.PassingThreshold);
                        tl.ReplaceQuestions(test.Questions);
                        context.Set<TestLesson>().Add(tl);
                        break;
                }
            }
        }

        await context.SaveChangesAsync(ct);

        // Step 3 — reload with full navigation so Publish() can validate the in-memory
        // lesson collection. Section._lessons is a backing field populated by EF on load.
        var fullCourse = await context.Courses
            .Include(c => c.Sections)
            .ThenInclude(s => s.Lessons)
            .FirstAsync(c => c.Id == course.Id, ct);

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

        await context.SaveChangesAsync(ct);
    }
}



