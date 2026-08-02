using System.Net;
using System.Net.Http.Json;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>POST /api/courses/{courseId}/sections/{sectionId}/lessons/test.</summary>
public sealed class CreateTestLessonTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid sectionId) =>
        $"/api/courses/{courseId}/sections/{sectionId}/lessons/test";

    [Fact]
    public async Task The_owner_creates_a_test_lesson_and_it_appears_in_the_section()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var created = await course.Owner.PostAsJsonAsync(
            Url(course.CourseId, sectionId), CourseWorkspace.TestLessonBody("Quiz"));

        created.StatusCode.Should().Be(HttpStatusCode.Created, because: await created.Content.ReadAsStringAsync());
        var lesson = (await course.LessonsAsync(sectionId)).Should().ContainSingle(l => l.Title == "Quiz").Which;
        lesson.LessonType.Should().Be("Test");
    }

    [Fact]
    public async Task A_test_with_no_questions_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var body = new
        {
            title = "Quiz",
            description = (string?)null,
            attemptLimit = (int?)null,
            cooldownMinutes = (int?)null,
            passingThreshold = 70,
            reviewMode = "FullReview",
            questions = Array.Empty<object>(),
        };

        var response = await course.Owner.PostAsJsonAsync(Url(course.CourseId, sectionId), body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await App.Stranger().PostAsJsonAsync(
            Url(course.CourseId, sectionId), CourseWorkspace.TestLessonBody("Quiz"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
