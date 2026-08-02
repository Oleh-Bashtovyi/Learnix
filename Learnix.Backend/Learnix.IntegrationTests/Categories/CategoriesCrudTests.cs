using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Learnix.Domain.Constants;
using Learnix.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.IntegrationTests.Categories;

/// <summary>
/// The category endpoints, driven over HTTP through the real pipeline: routing, the <c>[Authorize]</c>
/// gates, the MediatR validation behavior, EF against Postgres, and the Redis cache the public list is
/// served from. These are the checks the deleted handler-level role tests could never make — that the
/// route attribute actually stops a student — plus the ones no unit test can: a real 409 from the unique
/// index, a real 400 from the pipeline, and cache invalidation across two requests.
/// </summary>
public sealed class CategoriesCrudTests(LearnixApp app) : IntegrationTestBase(app)
{
    private sealed record CategoryDto(Guid Id, string Name, string Slug, string? ImageUrl, int CoursesCount);
    private sealed record CreateBody(string Name, string Slug, string? ImageBlobPath = null);
    private sealed record UpdateBody(string Name, string Slug, string? ImageBlobPath, bool RemoveImage);

    // Authorization: the whole reason the handler-level checks were safe to delete

    [Fact]
    public async Task Creating_a_category_anonymously_is_unauthorized()
    {
        var response = await App.ClientWithRoles()
            .PostAsJsonAsync("/api/categories", new CreateBody("Backend", "backend"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Creating_a_category_as_a_student_is_forbidden_with_the_insufficient_role_code()
    {
        var response = await App.ClientWithRoles(Roles.Student)
            .PostAsJsonAsync("/api/categories", new CreateBody("Backend", "backend"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        CodeOf(problem).Should().Be("insufficient_role");
    }

    [Fact]
    public async Task Anyone_may_read_the_public_category_list()
    {
        var response = await App.ClientWithRoles().GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Create

    [Fact]
    public async Task An_admin_creates_a_category_and_it_appears_in_the_public_list()
    {
        var admin = App.ClientWithRoles(Roles.Admin);

        var created = await admin.PostAsJsonAsync("/api/categories", new CreateBody("Backend", "backend"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await App.ClientWithRoles().GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        list.Should().ContainSingle(c => c.Slug == "backend" && c.Name == "Backend");
    }

    [Fact]
    public async Task A_duplicate_slug_is_a_conflict()
    {
        var admin = App.ClientWithRoles(Roles.Admin);
        await admin.PostAsJsonAsync("/api/categories", new CreateBody("Backend", "backend"));

        var second = await admin.PostAsJsonAsync("/api/categories", new CreateBody("Back End", "backend"));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_empty_name_is_rejected_by_validation()
    {
        var response = await App.ClientWithRoles(Roles.Admin)
            .PostAsJsonAsync("/api/categories", new CreateBody("", "backend"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Update & delete

    [Fact]
    public async Task An_admin_renames_a_category_and_the_new_name_is_served()
    {
        var admin = App.ClientWithRoles(Roles.Admin);
        var id = await CreateAsync(admin, "Backend", "backend");

        var updated = await admin.PutAsJsonAsync($"/api/categories/{id}",
            new UpdateBody("Server Side", "backend", null, RemoveImage: false));

        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await App.ClientWithRoles().GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        list.Should().ContainSingle(c => c.Id == id && c.Name == "Server Side");
    }

    [Fact]
    public async Task An_admin_deletes_a_category_and_it_leaves_the_list()
    {
        var admin = App.ClientWithRoles(Roles.Admin);
        var id = await CreateAsync(admin, "Backend", "backend");

        var deleted = await admin.DeleteAsync($"/api/categories/{id}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await App.ClientWithRoles().GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        list.Should().NotContain(c => c.Id == id);
    }

    // Cache invalidation: only observable across two requests, so only an integration test sees it

    [Fact]
    public async Task Creating_a_category_invalidates_the_cached_public_list()
    {
        var admin = App.ClientWithRoles(Roles.Admin);
        var anon = App.ClientWithRoles();

        // Prime the Redis cache with the empty list.
        var before = await anon.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        before.Should().BeEmpty();

        await admin.PostAsJsonAsync("/api/categories", new CreateBody("Backend", "backend"));

        // If the handler did not evict CacheKeys.Categories.All, this still serves the stale empty list.
        var after = await anon.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        after.Should().ContainSingle(c => c.Slug == "backend");
    }

    private static async Task<Guid> CreateAsync(HttpClient admin, string name, string slug)
    {
        var response = await admin.PostAsJsonAsync("/api/categories", new CreateBody(name, slug));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private static string? CodeOf(ProblemDetails? problem) =>
        problem?.Extensions.TryGetValue("code", out var value) == true && value is JsonElement e
            ? e.GetString()
            : null;
}
