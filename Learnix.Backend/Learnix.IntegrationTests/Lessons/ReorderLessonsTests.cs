using System.Net;
using System.Net.Http.Json;
using Learnix.Application.Common.Models;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>
/// POST /api/courses/{courseId}/sections/{sectionId}/lessons/reorder. The case only a real database catches:
/// a swap puts two lessons on each other's DisplayOrder, tripping the unique (SectionId, DisplayOrder)
/// constraint per-row. It is DEFERRABLE (DatabaseObjects/ordering_deferrable.sql), so the permutation is
/// validated at COMMIT and succeeds — the coverage the in-memory unit tests could never provide.
/// </summary>
public sealed class ReorderLessonsTests(LearnixApp app) : IntegrationTestBase(app)
{
    private sealed record Body(IReadOnlyList<ReorderItem> Items);

    private static string Url(Guid courseId, Guid sectionId) =>
        $"/api/courses/{courseId}/sections/{sectionId}/lessons/reorder";

    [Fact]
    public async Task Reordering_swaps_the_display_order_of_two_lessons()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var first = await course.AddPostLessonAsync(sectionId, "First");   // order 0
        var second = await course.AddPostLessonAsync(sectionId, "Second"); // order 1

        var reorder = await course.Owner.PostAsJsonAsync(
            Url(course.CourseId, sectionId), new Body([new(first, 1), new(second, 0)]));

        reorder.StatusCode.Should().Be(HttpStatusCode.NoContent, because: await reorder.Content.ReadAsStringAsync());

        var lessons = await course.LessonsAsync(sectionId);
        lessons.Single(l => l.Id == first).Order.Should().Be(1);
        lessons.Single(l => l.Id == second).Order.Should().Be(0);
    }

    [Fact]
    public async Task An_empty_payload_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await course.Owner.PostAsJsonAsync(Url(course.CourseId, sectionId), new Body([]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_payload_that_omits_a_lesson_is_a_conflict()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var first = await course.AddPostLessonAsync(sectionId, "First");
        await course.AddPostLessonAsync(sectionId, "Second");

        // Item-level valid, but not the full set — the domain rejects it, mapped to 409 (not an unhandled 500).
        var response = await course.Owner.PostAsJsonAsync(
            Url(course.CourseId, sectionId), new Body([new(first, 0)]));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var first = await course.AddPostLessonAsync(sectionId, "First");
        var second = await course.AddPostLessonAsync(sectionId, "Second");

        var response = await App.Stranger().PostAsJsonAsync(
            Url(course.CourseId, sectionId), new Body([new(first, 1), new(second, 0)]));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
