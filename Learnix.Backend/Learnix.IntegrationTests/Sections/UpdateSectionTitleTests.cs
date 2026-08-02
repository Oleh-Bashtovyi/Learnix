using System.Net;
using System.Net.Http.Json;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Sections;

/// <summary>PATCH /api/courses/{courseId}/sections/{sectionId}.</summary>
public sealed class UpdateSectionTitleTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid sectionId) =>
        $"/api/courses/{courseId}/sections/{sectionId}";

    [Fact]
    public async Task The_owner_renames_a_section_and_the_new_title_is_served()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var updated = await course.Owner.PatchAsJsonAsync(
            Url(course.CourseId, sectionId), new { title = "Getting Started" });

        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await course.SectionsAsync()).Should().ContainSingle(s => s.Id == sectionId && s.Title == "Getting Started");
    }

    [Fact]
    public async Task An_empty_title_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await course.Owner.PatchAsJsonAsync(Url(course.CourseId, sectionId), new { title = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Renaming_an_unknown_section_is_not_found()
    {
        var course = await App.NewCourseAsync();

        var response = await course.Owner.PatchAsJsonAsync(
            Url(course.CourseId, Guid.NewGuid()), new { title = "Ghost" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await App.Stranger().PatchAsJsonAsync(
            Url(course.CourseId, sectionId), new { title = "Hijacked" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
