using System.Net;
using System.Net.Http.Json;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>PATCH /api/courses/{courseId}/lessons/{lessonId}/video.</summary>
public sealed class UpdateVideoLessonTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid lessonId) => $"/api/courses/{courseId}/lessons/{lessonId}/video";

    [Fact]
    public async Task The_owner_edits_a_video_lesson_and_the_new_title_is_served()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddVideoLessonAsync(sectionId, "Draft");

        var updated = await course.Owner.PatchAsJsonAsync(Url(course.CourseId, lessonId),
            new { title = "Final Cut", videoUrl = "temp-uploads/clip2", description = "Updated", durationSeconds = 120 });

        updated.StatusCode.Should().Be(HttpStatusCode.NoContent, because: await updated.Content.ReadAsStringAsync());
        (await course.LessonsAsync(sectionId)).Single(l => l.Id == lessonId).Title.Should().Be("Final Cut");
    }

    [Fact]
    public async Task An_empty_title_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddVideoLessonAsync(sectionId, "Draft");

        var response = await course.Owner.PatchAsJsonAsync(Url(course.CourseId, lessonId),
            new { title = "", videoUrl = "temp-uploads/clip2", description = (string?)null, durationSeconds = 120 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddVideoLessonAsync(sectionId, "Draft");

        var response = await App.Stranger().PatchAsJsonAsync(Url(course.CourseId, lessonId),
            new { title = "Hijacked", videoUrl = "temp-uploads/x", description = (string?)null, durationSeconds = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
