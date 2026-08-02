using System.Net;
using System.Net.Http.Json;
using Learnix.Application.Common.Models;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Sections;

/// <summary>
/// POST /api/courses/{courseId}/sections/reorder. The case only a real database catches: reorder swaps two
/// sections onto each other's DisplayOrder, which trips the unique (CourseId, DisplayOrder) constraint the
/// instant it is checked per-row. The constraint is DEFERRABLE (DatabaseObjects/ordering_deferrable.sql),
/// so the whole permutation is validated at COMMIT and the swap succeeds.
/// </summary>
public sealed class ReorderSectionsTests(LearnixApp app) : IntegrationTestBase(app)
{
    private sealed record Body(IReadOnlyList<ReorderItem> Items);

    private static string Url(Guid courseId) => $"/api/courses/{courseId}/sections/reorder";

    [Fact]
    public async Task Reordering_swaps_the_display_order_of_two_sections()
    {
        var course = await App.NewCourseAsync();
        var first = await course.AddSectionAsync("First");   // order 0
        var second = await course.AddSectionAsync("Second"); // order 1

        var reorder = await course.Owner.PostAsJsonAsync(
            Url(course.CourseId), new Body([new(first, 1), new(second, 0)]));

        reorder.StatusCode.Should().Be(HttpStatusCode.NoContent, because: await reorder.Content.ReadAsStringAsync());

        var sections = await course.SectionsAsync();
        sections.Single(s => s.Id == first).Order.Should().Be(1);
        sections.Single(s => s.Id == second).Order.Should().Be(0);
    }

    [Fact]
    public async Task An_empty_payload_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();

        var response = await course.Owner.PostAsJsonAsync(Url(course.CourseId), new Body([]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_payload_that_omits_a_section_is_a_conflict()
    {
        var course = await App.NewCourseAsync();
        var first = await course.AddSectionAsync("First");
        await course.AddSectionAsync("Second");

        // Item-level valid, but not the full set — the domain rejects it, mapped to 409 (not an unhandled 500).
        var response = await course.Owner.PostAsJsonAsync(Url(course.CourseId), new Body([new(first, 0)]));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var first = await course.AddSectionAsync("First");
        var second = await course.AddSectionAsync("Second");

        var response = await App.Stranger().PostAsJsonAsync(
            Url(course.CourseId), new Body([new(first, 1), new(second, 0)]));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
