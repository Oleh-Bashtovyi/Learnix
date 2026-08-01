using System.Net;
using System.Net.Http.Json;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Learnix.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>
/// The copy-on-write rule from ADR-BACK-LMS-006, end to end against a real database: a test's questions
/// live on a <see cref="TestVersion"/>, an edit reuses that version while nothing has been attempted
/// against it, and branches a new one the moment something has.
/// <para>
/// Asserted against the tables rather than a response body, because the whole mechanism is invisible
/// over HTTP — which is the point of it.
/// </para>
/// </summary>
public sealed class TestVersioningTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string CreateUrl(Guid courseId, Guid sectionId) =>
        $"/api/courses/{courseId}/sections/{sectionId}/lessons/test";

    private static string UpdateUrl(Guid courseId, Guid lessonId) =>
        $"/api/courses/{courseId}/lessons/{lessonId}/test";

    [Fact]
    public async Task Creating_a_test_lesson_creates_version_one_and_points_the_lesson_at_it()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var lessonId = await CreateTestAsync(course, sectionId, "Quiz", ("Capital of France?", "Paris", "Rome"));

        var versions = await VersionsOfAsync(lessonId);
        versions.Should().ContainSingle();
        versions[0].VersionNumber.Should().Be(1);
        versions[0].Questions.Should().ContainSingle().Which.Text.Should().Be("Capital of France?");

        (await LessonAsync(lessonId)).CurrentVersionId.Should().Be(versions[0].Id);
    }

    [Fact]
    public async Task Editing_only_the_settings_does_not_touch_the_version()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await CreateTestAsync(course, sectionId, "Draft", ("Capital of France?", "Paris", "Rome"));

        var before = (await VersionsOfAsync(lessonId)).Single();

        await UpdateTestAsync(course, lessonId, "Renamed", ("Capital of France?", "Paris", "Rome"));

        var after = await VersionsOfAsync(lessonId);
        after.Should().ContainSingle();
        after[0].Id.Should().Be(before.Id);
        after[0].VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task Editing_the_questions_of_a_test_nobody_has_attempted_reuses_the_same_version_row()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await CreateTestAsync(course, sectionId, "Draft", ("Capital of France?", "Paris", "Rome"));

        var before = (await VersionsOfAsync(lessonId)).Single();

        // Three saves in a row, each changing the questions. An instructor drafting a quiz must not leave
        // a trail of editions behind them.
        await UpdateTestAsync(course, lessonId, "Draft", ("Capital of Italy?", "Rome", "Paris"));
        await UpdateTestAsync(course, lessonId, "Draft", ("Capital of Spain?", "Madrid", "Lisbon"));
        await UpdateTestAsync(course, lessonId, "Draft", ("Capital of Japan?", "Tokyo", "Osaka"));

        var after = await VersionsOfAsync(lessonId);
        after.Should().ContainSingle();
        after[0].Id.Should().Be(before.Id);
        after[0].VersionNumber.Should().Be(1);
        after[0].Questions.Should().ContainSingle().Which.Text.Should().Be("Capital of Japan?");
    }

    [Fact]
    public async Task Editing_the_questions_of_an_attempted_test_branches_and_leaves_the_attempt_on_its_own_version()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await CreateTestAsync(course, sectionId, "Quiz", ("Capital of France?", "Paris", "Rome"));

        var original = (await VersionsOfAsync(lessonId)).Single();
        var attemptId = await AttemptAsync(course.CourseId, lessonId, original.Id);

        // The instructor swaps the options round — the edit that used to turn a right answer into a wrong one.
        await UpdateTestAsync(course, lessonId, "Quiz", ("Capital of France?", "Rome", "Paris"));

        var versions = await VersionsOfAsync(lessonId);
        versions.Should().HaveCount(2);

        var v1 = versions.Single(v => v.VersionNumber == 1);
        var v2 = versions.Single(v => v.VersionNumber == 2);

        // Version 1 is exactly what the student sat: "Paris" still first, still the correct one.
        v1.Id.Should().Be(original.Id);
        v1.Questions[0].Options[0].Text.Should().Be("Paris");
        v1.Questions[0].Options[0].IsCorrect.Should().BeTrue();

        v2.Questions[0].Options[0].Text.Should().Be("Rome");

        // The attempt still points at the edition it was taken against; the lesson has moved on.
        (await AttemptVersionIdAsync(attemptId)).Should().Be(v1.Id);
        (await LessonAsync(lessonId)).CurrentVersionId.Should().Be(v2.Id);
    }

    // Fixtures

    private static object Body(string title, (string Text, string First, string Second) question) => new
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
                text = question.Text,
                type = "SingleChoice",
                options = new[]
                {
                    new { text = question.First, isCorrect = true },
                    new { text = question.Second, isCorrect = false },
                },
                textAnswer = (object?)null,
            },
        },
    };

    private static async Task<Guid> CreateTestAsync(
        TestCourse course, Guid sectionId, string title, (string, string, string) question)
    {
        var response = await course.Owner.PostAsJsonAsync(
            CreateUrl(course.CourseId, sectionId), Body(title, question));

        response.StatusCode.Should().Be(HttpStatusCode.Created, because: await response.Content.ReadAsStringAsync());

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private static async Task UpdateTestAsync(
        TestCourse course, Guid lessonId, string title, (string, string, string) question)
    {
        var response = await course.Owner.PatchAsJsonAsync(
            UpdateUrl(course.CourseId, lessonId), Body(title, question));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, because: await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// An attempt written straight to the table. The rule under test is "a version with attempts is not
    /// overwritten", and the attempt row is its input — going through enrol, publish and pay to produce
    /// one would test those instead.
    /// </summary>
    private async Task<Guid> AttemptAsync(Guid courseId, Guid lessonId, Guid versionId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var attempt = TestAttempt.Create(courseId, lessonId, versionId, Guid.NewGuid(), attemptNumber: 1);
        db.TestAttempts.Add(attempt);
        await db.SaveChangesAsync();

        return attempt.Id;
    }

    private async Task<List<TestVersion>> VersionsOfAsync(Guid lessonId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.TestVersions
            .AsNoTracking()
            .Where(v => v.TestLessonId == lessonId)
            .OrderBy(v => v.VersionNumber)
            .ToListAsync();
    }

    private async Task<TestLesson> LessonAsync(Guid lessonId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.Lessons.AsNoTracking().OfType<TestLesson>().SingleAsync(l => l.Id == lessonId);
    }

    private async Task<Guid> AttemptVersionIdAsync(Guid attemptId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return (await db.TestAttempts.AsNoTracking().SingleAsync(a => a.Id == attemptId)).TestVersionId;
    }
}
