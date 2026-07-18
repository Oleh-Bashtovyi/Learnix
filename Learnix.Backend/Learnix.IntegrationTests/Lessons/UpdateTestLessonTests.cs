using System.Net;
using System.Net.Http.Json;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>PATCH /api/courses/{courseId}/lessons/{lessonId}/test.</summary>
public sealed class UpdateTestLessonTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid lessonId) => $"/api/courses/{courseId}/lessons/{lessonId}/test";

    [Fact]
    public async Task The_owner_edits_a_test_lesson_and_the_new_title_is_served()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddTestLessonAsync(sectionId, "Draft Quiz");

        var updated = await course.Owner.PatchAsJsonAsync(
            Url(course.CourseId, lessonId), CourseWorkspace.TestLessonBody("Final Quiz"));

        updated.StatusCode.Should().Be(HttpStatusCode.NoContent, because: await updated.Content.ReadAsStringAsync());
        (await course.LessonsAsync(sectionId)).Single(l => l.Id == lessonId).Title.Should().Be("Final Quiz");
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddTestLessonAsync(sectionId, "Draft Quiz");

        var response = await App.Stranger().PatchAsJsonAsync(
            Url(course.CourseId, lessonId), CourseWorkspace.TestLessonBody("Hijacked"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
