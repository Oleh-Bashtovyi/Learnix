using System.Net;
using System.Net.Http.Json;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>PATCH /api/courses/{courseId}/lessons/{lessonId}/post.</summary>
public sealed class UpdatePostLessonTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid lessonId) => $"/api/courses/{courseId}/lessons/{lessonId}/post";

    [Fact]
    public async Task The_owner_edits_a_post_lesson_and_the_new_content_is_served()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddPostLessonAsync(sectionId, "Draft");

        var updated = await course.Owner.PatchAsJsonAsync(
            Url(course.CourseId, lessonId), new { title = "Final", content = "Edited body." });

        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var lesson = (await course.LessonsAsync(sectionId)).Single(l => l.Id == lessonId);
        lesson.Title.Should().Be("Final");
        lesson.Content.Should().Be("Edited body.");
    }

    [Fact]
    public async Task An_empty_title_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddPostLessonAsync(sectionId, "Draft");

        var response = await course.Owner.PatchAsJsonAsync(
            Url(course.CourseId, lessonId), new { title = "", content = "Edited body." });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddPostLessonAsync(sectionId, "Draft");

        var response = await App.Stranger().PatchAsJsonAsync(
            Url(course.CourseId, lessonId), new { title = "Hijacked", content = "..." });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
