using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;
using Learnix.Infrastructure.Persistence;
using Learnix.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Learnix.Infrastructure.Services.CourseSeeders;

namespace Learnix.Infrastructure.Services;

/// <summary>
/// Opt-in dev seeder: creates a seed instructor and published courses across system categories.
/// Activate by setting SeedDevData:Enabled = true in appsettings.Development.json.
/// Idempotent вЂ” skips entirely if the seed instructor already owns any courses.
/// Each course contains PostLessons, VideoLessons, and TestLessons (all 3 types, в‰Ґ5 each).
/// Tests deliberately vary PassingThreshold and AttemptLimit for app-testing purposes.
/// </summary>
public sealed class CourseSeederHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IOptions<BlobStorageOptions> blobOptions,
    ILogger<CourseSeederHostedService> logger) : IHostedService
{
    private static readonly Learnix.Infrastructure.Services.CourseSeeders.SeedCourseDefinition[] SeedCourses =
    [
        Learnix.Infrastructure.Services.CourseSeeders.CSharpFundamentalsSeeder.GetDefinition(),
        Learnix.Infrastructure.Services.CourseSeeders.DesignPatternsSeeder.GetDefinition(),
        Learnix.Infrastructure.Services.CourseSeeders.React19Seeder.GetDefinition(),
        Learnix.Infrastructure.Services.CourseSeeders.PythonDataAnalysisSeeder.GetDefinition(),
        Learnix.Infrastructure.Services.CourseSeeders.UiUxDesignSeeder.GetDefinition(),
        Learnix.Infrastructure.Services.CourseSeeders.SqlDatabaseDesignSeeder.GetDefinition(),
        Learnix.Infrastructure.Services.CourseSeeders.NodeJsRestApiSeeder.GetDefinition(),
        Learnix.Infrastructure.Services.CourseSeeders.DigitalMarketingSeeder.GetDefinition(),
        Learnix.Infrastructure.Services.CourseSeeders.AdvancedAlgorithmsSeeder.GetDefinition(),
        Learnix.Infrastructure.Services.CourseSeeders.CloudArchitectureSeeder.GetDefinition()
    ];
    // в”Ђв”Ђ IHostedService в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var enabled = configuration["SeedData:Enabled"];

        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            return;

        var email = configuration["SeedData:InstructorEmail"] ?? "instructor@learnix.dev";
        var password = configuration["SeedData:InstructorPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Course seeder: SeedData:InstructorPassword is not set вЂ” skipping course seeding.");
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

        if (alreadySeeded)
        {
            logger.LogInformation(
                "Course seeder: courses already exist for {Email} вЂ” skipping.", email);
            return;
        }



        var categoryIdBySlug = await context.Categories
            .Where(c => c.IsSystem)
            .ToDictionaryAsync(c => c.Slug, c => c.Id, cancellationToken);

        var seededCount = 0;
        foreach (var definition in SeedCourses)
        {
            if (!categoryIdBySlug.TryGetValue(definition.CategorySlug, out var categoryId))
            {
                logger.LogWarning(
                    "Course seeder: category '{Slug}' not found вЂ” skipping '{Title}'.",
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // в”Ђв”Ђ Private helpers в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

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
        // Step 1 вЂ” persist course + sections so their IDs are stable before adding lessons.
        var course = Course.Create(
            instructorId, categoryId,
            definition.Title, definition.Description,
            definition.Price, definition.Tags);

        var coverPath = $"{blobOptions.Value.CourseCoverContainer}/{Guid.NewGuid()}-cover.png";
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream($"Learnix.Infrastructure.Assets.{definition.ImageName}");
            if (stream != null)
            {
                await blobStorage.UploadAsync(coverPath, stream, "image/png", ct);
                await blobStorage.MarkConfirmedAsync(coverPath, ct);
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

        // Step 2 вЂ” add lessons via DbContext (Section.AddLesson is internal to the Domain assembly).
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
                            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                            using var stream = assembly.GetManifestResourceStream("Learnix.Infrastructure.Assets.placeholder.mp4");
                            if (stream != null)
                            {
                                await blobStorage.UploadAsync(videoPath, stream, "video/mp4", ct);
                                await blobStorage.MarkConfirmedAsync(videoPath, ct);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to upload placeholder video for {Title}", vid.Title);
                        }

                        var vl = VideoLesson.Create(
                            section.Id, vid.Title, order, videoPath, vid.Description);
                        vl.SetVisibility(false);
                        context.Set<VideoLesson>().Add(vl);
                        break;

                    case SeedTest test:
                        var tl = TestLesson.Create(
                            section.Id, test.Title, order,
                            test.Description, test.AttemptLimit,
                            test.CooldownMinutes, test.PassingThreshold);
                        tl.ReplaceQuestions(test.Questions);
                        tl.SetVisibility(false);
                        context.Set<TestLesson>().Add(tl);
                        break;
                }
            }
        }

        await context.SaveChangesAsync(ct);

        // Step 3 вЂ” reload with full navigation so Publish() can validate the in-memory
        // lesson collection. Section._lessons is a backing field populated by EF on load.
        var fullCourse = await context.Courses
            .Include(c => c.Sections)
            .ThenInclude(s => s.Lessons)
            .FirstAsync(c => c.Id == course.Id, ct);

        fullCourse.Publish();

        await context.SaveChangesAsync(ct);
    }
}
