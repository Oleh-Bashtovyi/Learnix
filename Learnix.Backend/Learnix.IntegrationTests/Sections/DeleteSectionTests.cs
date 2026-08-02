using System.Net;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Sections;

/// <summary>DELETE /api/courses/{courseId}/sections/{sectionId}.</summary>
public sealed class DeleteSectionTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid sectionId) =>
        $"/api/courses/{courseId}/sections/{sectionId}";

    [Fact]
    public async Task The_owner_deletes_a_section_and_it_leaves_the_course()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var deleted = await course.Owner.DeleteAsync(Url(course.CourseId, sectionId));

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await course.SectionsAsync()).Should().NotContain(s => s.Id == sectionId);
    }

    [Fact]
    public async Task Deleting_an_unknown_section_is_not_found()
    {
        var course = await App.NewCourseAsync();

        var response = await course.Owner.DeleteAsync(Url(course.CourseId, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await App.Stranger().DeleteAsync(Url(course.CourseId, sectionId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
