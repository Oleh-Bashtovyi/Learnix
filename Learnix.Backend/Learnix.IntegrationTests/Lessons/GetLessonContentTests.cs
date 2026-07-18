using System.Net;
using Learnix.Domain.Constants;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>
/// GET /api/courses/{courseId}/lessons/{lessonId} — the one read endpoint here, and the only one gated by
/// <em>enrollment</em> rather than ownership: the handler rejects anyone without an active enrollment
/// before it even looks the lesson up. Its content-read happy path belongs to the learning flow (publish →
/// enrol → read) and is covered there; this suite pins the endpoint's own guards.
/// </summary>
public sealed class GetLessonContentTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid lessonId) => $"/api/courses/{courseId}/lessons/{lessonId}";

    [Fact]
    public async Task Reading_lesson_content_anonymously_is_unauthorized()
    {
        var response = await App.ClientWithRoles().GetAsync(Url(Guid.NewGuid(), Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_student_who_is_not_enrolled_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddPostLessonAsync(sectionId, "Lesson");

        var outsider = App.ClientForUser(Guid.NewGuid(), Roles.Student);
        var response = await outsider.GetAsync(Url(course.CourseId, lessonId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
