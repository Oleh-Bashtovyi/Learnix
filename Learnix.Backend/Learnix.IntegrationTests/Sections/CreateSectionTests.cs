using System.Net;
using System.Net.Http.Json;
using Learnix.Domain.Constants;
using Learnix.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.IntegrationTests.Sections;

/// <summary>
/// POST /api/courses/{courseId}/sections. This endpoint carries both authorization layers — a role gate on
/// the route and an ownership check in the handler (ADR-BACK-AUTH-013) — so the full matrix is exercised
/// here, on the representative create; the other section endpoints assert the ownership check alone.
/// </summary>
public sealed class CreateSectionTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId) => $"/api/courses/{courseId}/sections";

    [Fact]
    public async Task The_owner_creates_a_section_and_it_appears_on_the_course()
    {
        var course = await App.NewCourseAsync();

        var created = await course.Owner.PostAsJsonAsync(Url(course.CourseId), new { title = "Intro" });

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        (await course.SectionsAsync()).Should().ContainSingle(s => s.Title == "Intro");
    }

    [Fact]
    public async Task An_empty_title_is_rejected_by_validation()
    {
        var course = await App.NewCourseAsync();

        var response = await course.Owner.PostAsJsonAsync(Url(course.CourseId), new { title = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Creating_a_section_anonymously_is_unauthorized()
    {
        var course = await App.NewCourseAsync();

        var response = await App.ClientWithRoles().PostAsJsonAsync(Url(course.CourseId), new { title = "Intro" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Creating_a_section_as_a_student_is_forbidden_by_the_route()
    {
        var course = await App.NewCourseAsync();

        var response = await App.ClientWithRoles(Roles.Student)
            .PostAsJsonAsync(Url(course.CourseId), new { title = "Intro" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.CodeOf().Should().Be("insufficient_role");
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden_by_the_handler()
    {
        var course = await App.NewCourseAsync();

        var response = await App.Stranger().PostAsJsonAsync(Url(course.CourseId), new { title = "Intro" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_admin_may_create_a_section_on_a_course_they_do_not_own()
    {
        var course = await App.NewCourseAsync();

        var admin = App.ClientForUser(Guid.NewGuid(), Roles.Admin);
        var response = await admin.PostAsJsonAsync(Url(course.CourseId), new { title = "Intro" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
