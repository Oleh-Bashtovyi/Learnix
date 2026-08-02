using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Courses.Queries.GetPublicCourses;
using Learnix.Domain.Constants;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Courses;

/// <summary>
/// The public catalog search over real HTTP and real Postgres. Unlike a handler-level unit test (which
/// substitutes <c>IPublicCourseCatalogSearchService</c> and never touches EF's LINQ translator), this is
/// what actually exercises <c>CourseFullTextSearchExtensions</c>' <c>EF.Functions.WebSearchToTsQuery</c>
/// call against the query provider — see ADR-BACK-CATALOG-001. A prior version of that extension computed
/// the tsquery outside the `Where` lambda; it built and passed every unit test (the real query provider is
/// never involved) and only threw at runtime, once EF actually tried to translate it.
/// <para>
/// Courses here are owned by a real, persisted instructor (<see cref="LearnixApp.ClientForRegisteredUserAsync"/>),
/// not <see cref="CourseWorkspace.NewCourseAsync"/>'s throwaway JWT id.
/// <c>PublicCourseCatalogSearchService</c> inner-joins to <c>Users</c> to resolve
/// <c>InstructorFullName</c>, so a course whose <c>InstructorId</c> has no matching row is silently
/// dropped from the result list even though it was counted — <c>TotalCount</c> says 1, <c>Items</c> comes
/// back empty. That is exactly the shape a throwaway instructor id produces, and exactly why every other
/// suite's use of the cheaper helper is fine for them (nothing else in this codebase joins through to the
/// instructor's own row) but would be a false failure here.
/// </para>
/// </summary>
public sealed class CourseCatalogSearchTests(LearnixApp app) : IntegrationTestBase(app)
{
    [Fact]
    public async Task Searching_finds_a_published_course_by_a_word_in_its_title()
    {
        await (await NewCourseAsync()).PublishAsync();

        var response = await App.ClientWithRoles().GetAsync("/api/courses?search=architecture");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PaginatedResult<PublicCourseCardDto>>();
        page!.Items.Should().ContainSingle(c => c.Title == "Clean Architecture");
    }

    [Fact]
    public async Task Searching_does_not_return_an_unpublished_course()
    {
        // Left in Draft — never published.
        await NewCourseAsync();

        var response = await App.ClientWithRoles().GetAsync("/api/courses?search=architecture");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PaginatedResult<PublicCourseCardDto>>();
        page!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task A_search_with_no_matches_returns_an_empty_page_rather_than_failing()
    {
        await (await NewCourseAsync()).PublishAsync();

        var response = await App.ClientWithRoles().GetAsync("/api/courses?search=quantumcryptography");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PaginatedResult<PublicCourseCardDto>>();
        page!.Items.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="CourseWorkspace.NewCourseAsync"/>, except the owner is a real persisted <c>User</c> row
    /// (see the class remarks for why that matters here) rather than a throwaway JWT id.
    /// </summary>
    private async Task<TestCourse> NewCourseAsync()
    {
        var admin = App.ClientWithRoles(Roles.Admin);
        await admin.PostAsJsonAsync("/api/categories", new { name = "Backend", slug = "backend" });
        var categories = await App.ClientWithRoles().GetFromJsonAsync<List<JsonElement>>("/api/categories");
        var categoryId = categories!.Single().GetProperty("id").GetGuid();

        var owner = await App.ClientForRegisteredUserAsync(Roles.Instructor);
        var created = await owner.PostAsJsonAsync("/api/courses", new
        {
            categoryId,
            title = "Clean Architecture",
            description = "A course about keeping the layers honest.",
            price = 0m,
            tags = (string[]?)null,
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var courseId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("courseId").GetGuid();
        return new TestCourse(owner, courseId, categoryId);
    }
}
