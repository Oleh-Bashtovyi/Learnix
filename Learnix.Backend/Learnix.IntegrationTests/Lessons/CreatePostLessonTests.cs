using System.Net;
using System.Net.Http.Json;
using Learnix.Domain.Constants;
using Learnix.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>
/// POST /api/courses/{courseId}/sections/{sectionId}/lessons/post. The lesson-create endpoints share the
/// same two authorization layers (route role gate + handler ownership); the full matrix is exercised on the
/// representative post-create here, the other create endpoints assert the ownership check alone.
/// </summary>
public sealed class CreatePostLessonTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid sectionId) =>
        $"/api/courses/{courseId}/sections/{sectionId}/lessons/post";

    [Fact]
    public async Task The_owner_creates_a_post_lesson_and_it_appears_in_the_section()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var created = await course.Owner.PostAsJsonAsync(
            Url(course.CourseId, sectionId), new { title = "Welcome", content = "Read me." });

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var lesson = (await course.LessonsAsync(sectionId)).Should().ContainSingle(l => l.Title == "Welcome").Which;
        lesson.LessonType.Should().Be("Post");
        lesson.Content.Should().Be("Read me.");
    }

    [Fact]
    public async Task An_empty_title_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await course.Owner.PostAsJsonAsync(
            Url(course.CourseId, sectionId), new { title = "", content = "Read me." });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_empty_content_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await course.Owner.PostAsJsonAsync(
            Url(course.CourseId, sectionId), new { title = "Welcome", content = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Creating_a_lesson_anonymously_is_unauthorized()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await App.ClientWithRoles().PostAsJsonAsync(
            Url(course.CourseId, sectionId), new { title = "Welcome", content = "Read me." });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Creating_a_lesson_as_a_student_is_forbidden_by_the_route()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await App.ClientWithRoles(Roles.Student).PostAsJsonAsync(
            Url(course.CourseId, sectionId), new { title = "Welcome", content = "Read me." });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.CodeOf().Should().Be("insufficient_role");
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden_by_the_handler()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");

        var response = await App.Stranger().PostAsJsonAsync(
            Url(course.CourseId, sectionId), new { title = "Welcome", content = "Read me." });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
