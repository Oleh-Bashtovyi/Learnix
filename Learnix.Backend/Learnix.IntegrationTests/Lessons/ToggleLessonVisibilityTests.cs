using System.Net;
using System.Net.Http.Json;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>PATCH /api/courses/{courseId}/lessons/{lessonId}/toggle-visibility.</summary>
public sealed class ToggleLessonVisibilityTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid lessonId) =>
        $"/api/courses/{courseId}/lessons/{lessonId}/toggle-visibility";

    [Fact]
    public async Task The_owner_hides_a_lesson_and_it_is_marked_hidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddPostLessonAsync(sectionId, "Lesson");

        var toggled = await course.Owner.PatchAsJsonAsync(
            Url(course.CourseId, lessonId), new { isVisible = false });

        toggled.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await course.LessonsAsync(sectionId)).Single(l => l.Id == lessonId).IsHidden.Should().BeTrue();
    }

    [Fact]
    public async Task A_missing_visibility_flag_is_rejected()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddPostLessonAsync(sectionId, "Lesson");

        var response = await course.Owner.PatchAsJsonAsync(Url(course.CourseId, lessonId), new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddPostLessonAsync(sectionId, "Lesson");

        var response = await App.Stranger().PatchAsJsonAsync(
            Url(course.CourseId, lessonId), new { isVisible = false });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
