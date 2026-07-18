using System.Net;
using System.Net.Http.Json;
using Learnix.Domain.Constants;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Wishlist;

/// <summary>
/// The wishlist endpoints, driven over HTTP through the real pipeline: the <c>[Authorize]</c> gate, the
/// handlers' own rules (published-only, no duplicates, enrollment wins), and EF against Postgres —
/// including the real foreign key from <c>WishlistItems.UserId</c> to <c>AspNetUsers</c>, which is why the
/// acting student here is always a persisted user (<see cref="LearnixApp.ClientForRegisteredUserAsync"/>).
/// These are the checks a handler-level unit test cannot make on its own: that adding a course you're
/// already enrolled in is rejected by a real 409, and that enrolling clears the wishlist row across two
/// separate requests.
/// </summary>
public sealed class WishlistTests(LearnixApp app) : IntegrationTestBase(app)
{
    private sealed record WishlistCourseDto(
        Guid CourseId, string Title, string? CoverImageUrl, decimal Price, bool IsFree, DateTime AddedAt);

    private sealed record WishlistCountDto(int Count);

    private sealed record PagedResult<T>(int Page, int PageSize, long TotalCount, IReadOnlyList<T> Items);

    // Authorization

    [Fact]
    public async Task Reading_the_wishlist_anonymously_is_unauthorized()
    {
        var response = await App.ClientWithRoles().GetAsync("/api/wishlist");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Adding_to_the_wishlist_anonymously_is_unauthorized()
    {
        var response = await App.ClientWithRoles().PostAsync($"/api/wishlist/{Guid.NewGuid()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Add

    [Fact]
    public async Task A_student_adds_a_published_course_to_their_wishlist()
    {
        var course = await (await App.NewCourseAsync()).PublishAsync();
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);

        var added = await student.PostAsync($"/api/wishlist/{course.CourseId}", null);

        added.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var mine = await student.GetFromJsonAsync<PagedResult<WishlistCourseDto>>("/api/wishlist");
        mine!.Items.Should().ContainSingle(w => w.CourseId == course.CourseId);
    }

    [Fact]
    public async Task Adding_the_same_course_twice_does_not_duplicate_it()
    {
        var course = await (await App.NewCourseAsync()).PublishAsync();
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);

        await student.PostAsync($"/api/wishlist/{course.CourseId}", null);
        var second = await student.PostAsync($"/api/wishlist/{course.CourseId}", null);

        second.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var mine = await student.GetFromJsonAsync<PagedResult<WishlistCourseDto>>("/api/wishlist");
        mine!.Items.Should().ContainSingle(w => w.CourseId == course.CourseId);
    }

    [Fact]
    public async Task Adding_an_unpublished_course_is_not_found()
    {
        // NewCourseAsync leaves the course in Draft — never published.
        var course = await App.NewCourseAsync();
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);

        var response = await student.PostAsync($"/api/wishlist/{course.CourseId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Adding_a_nonexistent_course_is_not_found()
    {
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);

        var response = await student.PostAsync($"/api/wishlist/{Guid.NewGuid()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Adding_a_course_the_student_is_already_enrolled_in_is_a_conflict()
    {
        var course = await (await App.NewCourseAsync()).PublishAsync();
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);
        await student.PostAsJsonAsync("/api/enrollments", new { courseId = course.CourseId });

        var response = await student.PostAsync($"/api/wishlist/{course.CourseId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // Remove

    [Fact]
    public async Task A_student_removes_a_course_from_their_wishlist()
    {
        var course = await (await App.NewCourseAsync()).PublishAsync();
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);
        await student.PostAsync($"/api/wishlist/{course.CourseId}", null);

        var removed = await student.DeleteAsync($"/api/wishlist/{course.CourseId}");

        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var mine = await student.GetFromJsonAsync<PagedResult<WishlistCourseDto>>("/api/wishlist");
        mine!.Items.Should().NotContain(w => w.CourseId == course.CourseId);
    }

    [Fact]
    public async Task Removing_a_course_that_was_never_wishlisted_still_succeeds()
    {
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);

        var response = await student.DeleteAsync($"/api/wishlist/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // Listing & scoping

    [Fact]
    public async Task An_empty_wishlist_returns_an_empty_page()
    {
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);

        var mine = await student.GetFromJsonAsync<PagedResult<WishlistCourseDto>>("/api/wishlist");

        mine!.Items.Should().BeEmpty();
        mine.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Wishlisted_courses_carry_their_price_and_free_flag()
    {
        var free = await (await App.NewCourseAsync()).PublishAsync();
        var paid = await (await App.NewCourseAsync()).PublishAsync(price: 49.99m);
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);
        await student.PostAsync($"/api/wishlist/{free.CourseId}", null);
        await student.PostAsync($"/api/wishlist/{paid.CourseId}", null);

        var mine = await student.GetFromJsonAsync<PagedResult<WishlistCourseDto>>("/api/wishlist");

        mine!.Items.Should().ContainSingle(w => w.CourseId == free.CourseId && w.IsFree && w.Price == 0m);
        mine.Items.Should()
            .ContainSingle(w => w.CourseId == paid.CourseId && !w.IsFree && w.Price == 49.99m);
    }

    [Fact]
    public async Task A_students_wishlist_never_shows_another_students_items()
    {
        var course = await (await App.NewCourseAsync()).PublishAsync();
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);
        await student.PostAsync($"/api/wishlist/{course.CourseId}", null);

        var otherStudent = await App.ClientForRegisteredUserAsync(Roles.Student);
        var mine = await otherStudent.GetFromJsonAsync<PagedResult<WishlistCourseDto>>("/api/wishlist");

        mine!.Items.Should().BeEmpty();
    }

    // Count

    [Fact]
    public async Task The_wishlist_count_reflects_additions_and_removals()
    {
        var first = await (await App.NewCourseAsync()).PublishAsync();
        var second = await (await App.NewCourseAsync()).PublishAsync();
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);

        await student.PostAsync($"/api/wishlist/{first.CourseId}", null);
        await student.PostAsync($"/api/wishlist/{second.CourseId}", null);
        (await student.GetFromJsonAsync<WishlistCountDto>("/api/wishlist/count"))!.Count.Should().Be(2);

        await student.DeleteAsync($"/api/wishlist/{first.CourseId}");
        (await student.GetFromJsonAsync<WishlistCountDto>("/api/wishlist/count"))!.Count.Should().Be(1);
    }

    // Cross-feature: only observable across two requests against two different controllers

    [Fact]
    public async Task Enrolling_in_a_wishlisted_course_removes_it_from_the_wishlist()
    {
        var course = await (await App.NewCourseAsync()).PublishAsync();
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);
        await student.PostAsync($"/api/wishlist/{course.CourseId}", null);

        var enrolled = await student.PostAsJsonAsync("/api/enrollments", new { courseId = course.CourseId });
        enrolled.StatusCode.Should().Be(HttpStatusCode.OK);

        var mine = await student.GetFromJsonAsync<PagedResult<WishlistCourseDto>>("/api/wishlist");
        mine!.Items.Should().BeEmpty();
        (await student.GetFromJsonAsync<WishlistCountDto>("/api/wishlist/count"))!.Count.Should().Be(0);
    }
}
