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
/// Opt-in dev seeder: creates a seed instructor and published courses across system categories.
/// Activate by setting SeedDevData:Enabled = true in appsettings.Development.json.
/// Idempotent — skips entirely if the seed instructor already owns any courses.
/// </summary>
public sealed class DevCourseSeederHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IOptions<BlobStorageOptions> blobOptions,
    ILogger<DevCourseSeederHostedService> logger) : IHostedService
{
    // 100×100 blue (#3B82F6) → white horizontal gradient
    private static readonly byte[] PlaceholderCoverPng = Convert.FromBase64String(
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
    private record SeedLesson(string Title, string Content);
    private record SeedSection(string Title, SeedLesson[] Lessons);
    private record SeedCourseDefinition(
        string CategorySlug,
        string Title,
        string Description,
        decimal Price,
        string[] Tags,
        SeedSection[] Sections);

    private static readonly SeedCourseDefinition[] SeedCourses =
    [
        new("programming",
            "C# Fundamentals",
            "A complete introduction to C# for beginners. Learn variables, control flow, OOP, and the basics of the .NET ecosystem.",
            0m,
            ["csharp", "beginner", "dotnet"],
            [
                new("Getting Started", [
                    new("Introduction to C#",
                        "C# is a modern, statically-typed language built on .NET. It compiles to IL and runs on the CLR, giving you cross-platform support and a rich standard library."),
                    new("Setting Up Your Environment",
                        "Install the .NET 8 SDK from dotnet.microsoft.com. Use Visual Studio 2022 (Community is free) or VS Code with the C# Dev Kit extension. Run `dotnet new console -n Hello` to scaffold your first project."),
                    new("Hello World",
                        "In modern .NET you don't need a Main method — a top-level statement is the entry point: `Console.WriteLine(\"Hello, World!\");`. Build and run with `dotnet run`."),
                ]),
                new("Core Language Concepts", [
                    new("Variables and Data Types",
                        "C# is strongly typed. Value types (int, bool, decimal, struct) live on the stack; reference types (string, class, array) live on the heap. Use `var` to let the compiler infer the type."),
                    new("Control Flow",
                        "Use `if`/`else` for branching, `switch` expressions for pattern matching, and `for`/`foreach`/`while` for loops. Prefer `foreach` over index-based loops when you don't need the index."),
                    new("Methods",
                        "Methods define reusable behavior. Parameters are passed by value by default; use `ref` or `out` to pass by reference. C# supports optional parameters and named arguments."),
                ]),
            ]),

        new("programming",
            "Design Patterns in C#",
            "Learn the most important Gang of Four design patterns with practical C# examples. Assumes basic OOP knowledge.",
            29.99m,
            ["csharp", "design-patterns", "intermediate"],
            [
                new("Creational Patterns", [
                    new("Singleton",
                        "Ensures a class has only one instance. In modern C#, prefer the DI container (singleton lifetime) — it is thread-safe and testable. Avoid manual double-checked locking."),
                    new("Factory Method",
                        "Defines an interface for creating objects and lets subclasses decide the concrete type. In C# typically implemented as a static `Create()` factory or an abstract factory interface registered in DI."),
                ]),
                new("Structural Patterns", [
                    new("Repository Pattern",
                        "Abstracts data access behind an interface. Handlers depend on ICourseRepository, not EF Core directly — making unit testing straightforward and future persistence changes isolated."),
                    new("Decorator Pattern",
                        "Attaches additional behavior to an object dynamically by wrapping it. ASP.NET Core middleware and IHttpClientFactory pipelines are decorator chains. Use Scrutor for easy decorator registration in DI."),
                ]),
            ]),

        new("web-development",
            "React 19 with TypeScript",
            "Build modern, type-safe web UIs with React 19, hooks, TanStack Query, and Zustand. Covers component patterns, state management, and API integration.",
            19.99m,
            ["react", "typescript", "frontend", "tanstack-query"],
            [
                new("React Fundamentals", [
                    new("Components and JSX",
                        "React components are functions that return JSX — syntactic sugar over React.createElement. TypeScript adds type safety: `const Btn = ({ label }: { label: string }) => <button>{label}</button>;`"),
                    new("Hooks: useState and useEffect",
                        "`useState` manages local state. `useEffect` syncs with external systems. The dependency array controls when effects re-run. Never put server data in useState — use TanStack Query instead."),
                    new("Props and Composition",
                        "Props flow down; events flow up. Prefer composition (children prop, slot pattern) over deep prop drilling. Use Context only for slow-changing values like theme or locale."),
                ]),
                new("State and Data Fetching", [
                    new("Server State with TanStack Query",
                        "TanStack Query manages fetching, caching, and syncing server state. `useQuery` fetches data; `useMutation` sends writes. The query key is the cache identifier — keep it stable and descriptive."),
                    new("Client State with Zustand",
                        "Zustand stores UI-only state: theme, modal open/close, selected item. Create a store with `create<T>()`, use selectors to avoid unnecessary re-renders. One store per concern."),
                ]),
            ]),

        new("data-science",
            "Python for Data Analysis",
            "Master pandas, NumPy, and matplotlib for real-world data analysis tasks. Assumes basic Python knowledge.",
            24.99m,
            ["python", "pandas", "numpy", "data-science"],
            [
                new("Data Wrangling with Pandas", [
                    new("DataFrames and Series",
                        "A DataFrame is a 2D labeled data structure. Create one from a dict, CSV, or DB query. Inspect with `.head()`, `.info()`, `.describe()`. Index enables fast label-based access."),
                    new("Filtering and Selecting",
                        "Boolean indexing: `df[df['age'] > 30]`. `.loc[]` selects by label; `.iloc[]` by position. Chain operations with method chaining for readable pipelines."),
                    new("Grouping and Aggregation",
                        "`.groupby()` splits data into groups. Apply `.mean()`, `.sum()`, or `.agg({'col': 'sum'})`. Pivot tables provide Excel-style cross-tabulation."),
                ]),
                new("Visualization", [
                    new("Matplotlib Basics",
                        "`plt.plot()` for line charts, `plt.bar()` for bar charts, `plt.scatter()` for scatter plots. Always label axes and add a title. Use `fig, ax = plt.subplots()` for multi-panel figures."),
                    new("EDA Workflow",
                        "Steps: (1) inspect shape/dtypes, (2) check nulls/duplicates, (3) distribution histograms, (4) categorical value counts, (5) correlation heatmap, (6) identify outliers. Document findings before modeling."),
                ]),
            ]),

        new("design",
            "UI/UX Design Principles",
            "Learn the fundamentals of user interface and user experience design. Covers visual design, information architecture, and prototyping in Figma.",
            14.99m,
            ["design", "ux", "ui", "figma"],
            [
                new("Visual Design Foundations", [
                    new("Color Theory for UI",
                        "Use a primary, secondary, and semantic palette (success/warning/error). Ensure 4.5:1 contrast ratio for body text (WCAG AA). HSL makes it easy to create tints — adjust L while keeping H consistent."),
                    new("Typography and Hierarchy",
                        "Limit typefaces to two: one for headings, one for body. Use a typographic scale (1.25x ratio). Establish hierarchy through size, weight, and spacing — not color alone. Aim for 60–80 characters per line."),
                ]),
                new("UX Process", [
                    new("User Research Methods",
                        "Qualitative (interviews, contextual inquiry) answers WHY. Quantitative (surveys, A/B tests) answers WHAT. A usability test with 5 users reveals 85% of major issues."),
                    new("Wireframing in Figma",
                        "Start with low-fidelity wireframes to explore structure, then add visual design in high-fidelity. Figma Auto Layout enables responsive components. Use components and variants to build a consistent design system."),
                ]),
            ]),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var enabled = configuration["SeedDevData:Enabled"];
        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            return;

        var email = configuration["SeedDevData:InstructorEmail"] ?? "instructor@learnix.dev";
        var password = configuration["SeedDevData:InstructorPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Dev seeder: SeedDevData:InstructorPassword is not set — skipping course seeding.");
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
                "Dev seeder: courses already exist for {Email} — skipping.", email);
            return;
        }

        var placeholderPath = $"{blobOptions.Value.CourseCoverContainer}/seed-placeholder.png";
        try
        {
            await blobStorage.UploadAsync(
                placeholderPath,
                new MemoryStream(PlaceholderCoverPng),
                "image/png",
                cancellationToken);
            await blobStorage.MarkConfirmedAsync(placeholderPath, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Dev seeder: could not upload placeholder cover to '{Path}' — is blob storage running?",
                placeholderPath);
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
                    "Dev seeder: category '{Slug}' not found — skipping '{Title}'.",
                    definition.CategorySlug, definition.Title);
                continue;
            }

            await SeedSingleCourseAsync(context, instructor.Id, categoryId, definition, placeholderPath, cancellationToken);
            seededCount++;
        }

        logger.LogInformation(
            "Dev seeder: seeded {Count} courses for instructor {Email}.", seededCount, email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // -------------------------------------------------------------------------

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
        string placeholderPath,
        CancellationToken ct)
    {
        // Step 1 — persist course + sections so their IDs are stable before adding lessons.
        var course = Course.Create(
            instructorId, categoryId,
            definition.Title, definition.Description,
            definition.Price, definition.Tags);

        context.Courses.Add(course);

        var sections = definition.Sections
            .Select(s => (Entity: course.AddSection(s.Title), Def: s))
            .ToList();

        await context.SaveChangesAsync(ct);

        // Step 2 — add lessons directly via DbContext (Section.AddLesson is internal to the
        // Domain assembly). The unique (SectionId, DisplayOrder) index requires stable order values.
        foreach (var (section, sectionDef) in sections)
        {
            for (var order = 0; order < sectionDef.Lessons.Length; order++)
            {
                var lessonDef = sectionDef.Lessons[order];
                context.Set<PostLesson>().Add(
                    PostLesson.Create(section.Id, lessonDef.Title, order, lessonDef.Content));
            }
        }

        await context.SaveChangesAsync(ct);

        // Step 3 — reload with full navigation so Publish() can validate the in-memory
        // lesson collection. Section._lessons is a backing field populated by EF on load.
        var fullCourse = await context.Courses
            .Include(c => c.Sections)
            .ThenInclude(s => s.Lessons)
            .FirstAsync(c => c.Id == course.Id, ct);

        fullCourse.SetCoverImage(placeholderPath);
        fullCourse.Publish();

        await context.SaveChangesAsync(ct);
    }
}
