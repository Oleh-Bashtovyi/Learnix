using System.Net;
using System.Net.Http.Json;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>
/// POST /api/courses/{courseId}/sections/{sectionId}/lessons/video. The handler commits the uploaded blob
/// synchronously (ADR-BACK-BLOB-002); blob storage is stubbed to promote it, so the create path runs end
/// to end without a real container.
/// </summary>
public sealed class CreateVideoLessonTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid sectionId) =>
        $"/api/courses/{courseId}/sections/{sectionId}/lessons/video";

    [Fact]
    public async Task The_owner_creates_a_video_lesson_and_it_appears_in_the_section()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var created = await course.Owner.PostAsJsonAsync(Url(course.CourseId, sectionId),
            new { title = "Overview", videoUrl = "temp-uploads/clip", description = "Intro clip", durationSeconds = 90 });

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var lesson = (await course.LessonsAsync(sectionId)).Should().ContainSingle(l => l.Title == "Overview").Which;
        lesson.LessonType.Should().Be("Video");
        lesson.VideoUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task An_empty_title_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await course.Owner.PostAsJsonAsync(Url(course.CourseId, sectionId),
            new { title = "", videoUrl = "temp-uploads/clip", description = (string?)null, durationSeconds = 90 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_missing_video_blob_path_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await course.Owner.PostAsJsonAsync(Url(course.CourseId, sectionId),
            new { title = "Overview", videoUrl = "", description = (string?)null, durationSeconds = 90 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await App.Stranger().PostAsJsonAsync(Url(course.CourseId, sectionId),
            new { title = "Overview", videoUrl = "temp-uploads/clip", description = (string?)null, durationSeconds = 90 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
