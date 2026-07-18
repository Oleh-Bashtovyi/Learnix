using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Learnix.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.IntegrationTests.Infrastructure;

/// <summary>A course owned by a freshly-minted instructor — the client is that owner, so ownership checks
/// line up (course ownership is <c>InstructorId == currentUser.UserId</c>, ADR-BACK-AUTH-013).</summary>
internal sealed record TestCourse(HttpClient Owner, Guid CourseId);

// The slice of the course-for-edit view the authoring suites assert on. Enum-ish fields are kept as
// strings so the default HttpClient JSON options (no JsonStringEnumConverter) can read them.
internal sealed record EditCourse(Guid Id, IReadOnlyList<EditSection> Sections);
internal sealed record EditSection(Guid Id, string Title, int Order, IReadOnlyList<EditLesson> Lessons);
internal sealed record EditLesson(
    Guid Id, string Title, int Order, string LessonType, bool IsHidden,
    string? VideoUrl, string? Description, string? Content);

/// <summary>
/// HTTP fixtures shared by the course-authoring suites (sections, lessons): stand up a course, hang
/// sections and lessons off it, and read the edit view back — all over the real API. Kept in one place so
/// the per-endpoint test files don't each carry their own copy of the setup.
/// </summary>
internal static class CourseWorkspace
{
    public static async Task<TestCourse> NewCourseAsync(this LearnixApp app)
    {
        var admin = app.ClientWithRoles(Roles.Admin);
        await admin.PostAsJsonAsync("/api/categories", new { name = "Backend", slug = "backend" });
        var categories = await app.ClientWithRoles().GetFromJsonAsync<List<JsonElement>>("/api/categories");
        var categoryId = categories!.Single().GetProperty("id").GetGuid();

        var owner = app.ClientForUser(Guid.NewGuid(), Roles.Instructor);
        var created = await owner.PostAsJsonAsync("/api/courses", new
        {
            categoryId,
            title = "Clean Architecture",
            description = "A course about keeping the layers honest.",
            price = 0m,
            tags = (string[]?)null,
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var courseId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("courseId").GetGuid();
        return new TestCourse(owner, courseId);
    }

    /// <summary>A second instructor who owns nothing here — passes the route's role gate, fails the
    /// handler's ownership check. The seam between the two authorization layers.</summary>
    public static HttpClient Stranger(this LearnixApp app) => app.ClientForUser(Guid.NewGuid(), Roles.Instructor);

    public static async Task<Guid> AddSectionAsync(this TestCourse course, string title)
    {
        var response = await course.Owner
            .PostAsJsonAsync($"/api/courses/{course.CourseId}/sections", new { title });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return IdOf(await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    public static async Task<Guid> AddPostLessonAsync(this TestCourse course, Guid sectionId, string title)
    {
        var response = await course.Owner.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/sections/{sectionId}/lessons/post",
            new { title, content = "Body." });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return IdOf(await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    public static async Task<Guid> AddVideoLessonAsync(this TestCourse course, Guid sectionId, string title)
    {
        var response = await course.Owner.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/sections/{sectionId}/lessons/video",
            new { title, videoUrl = "temp-uploads/clip", description = (string?)null, durationSeconds = 60 });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return IdOf(await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    /// <summary>A minimal well-formed test-lesson request body (one single-choice question). Shared by the
    /// create/update test suites and <see cref="AddTestLessonAsync"/> so the payload lives in one place.</summary>
    public static object TestLessonBody(string title) => new
    {
        title,
        description = (string?)null,
        attemptLimit = (int?)null,
        cooldownMinutes = (int?)null,
        passingThreshold = 70,
        reviewMode = "FullReview",
        questions = new[]
        {
            new
            {
                text = "2 + 2 = ?",
                type = "SingleChoice",
                options = new[]
                {
                    new { text = "4", isCorrect = true },
                    new { text = "5", isCorrect = false },
                },
                textAnswer = (object?)null,
            },
        },
    };

    public static async Task<Guid> AddTestLessonAsync(this TestCourse course, Guid sectionId, string title)
    {
        var response = await course.Owner.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/sections/{sectionId}/lessons/test", TestLessonBody(title));
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: await response.Content.ReadAsStringAsync());
        return IdOf(await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    public static async Task<EditCourse> EditViewAsync(this TestCourse course)
    {
        var view = await course.Owner.GetFromJsonAsync<EditCourse>($"/api/courses/{course.CourseId}/edit");
        return view!;
    }

    public static async Task<IReadOnlyList<EditSection>> SectionsAsync(this TestCourse course) =>
        (await course.EditViewAsync()).Sections;

    public static async Task<IReadOnlyList<EditLesson>> LessonsAsync(this TestCourse course, Guid sectionId) =>
        (await course.EditViewAsync()).Sections.Single(s => s.Id == sectionId).Lessons;

    public static string? CodeOf(this ProblemDetails? problem) =>
        problem?.Extensions.TryGetValue("code", out var value) == true && value is JsonElement e
            ? e.GetString()
            : null;

    private static Guid IdOf(JsonElement body) => body.GetProperty("id").GetGuid();
}
