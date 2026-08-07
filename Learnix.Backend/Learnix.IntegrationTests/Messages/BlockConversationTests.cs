using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Learnix.Domain.Constants;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Messages;

/// <summary>
/// Both participants here are real, persisted users (<see cref="LearnixApp.ClientForRegisteredUserAsync"/>):
/// <c>CourseConversation.BlockedByUserId</c> has a hard FK to <c>AspNetUsers</c>, so
/// <see cref="CourseWorkspace.NewCourseAsync"/>'s throwaway instructor id would fail the INSERT the moment
/// a conversation is created for that course.
/// </summary>
public sealed class BlockConversationTests(LearnixApp app) : IntegrationTestBase(app)
{
    private sealed record ConversationDto(Guid Id, bool IsBlocked, bool BlockedByMe);
    private sealed record PagedResult<T>(int Page, int PageSize, long TotalCount, IReadOnlyList<T> Items);

    private async Task<(TestCourse Course, HttpClient Student, Guid ConversationId)> SetupAsync()
    {
        var course = await (await NewCourseAsync()).PublishAsync();
        var student = await App.ClientForRegisteredUserAsync(Roles.Student);

        var enrolled = await student.PostAsJsonAsync("/api/enrollments", new { courseId = course.CourseId });
        enrolled.StatusCode.Should().Be(HttpStatusCode.OK);

        var started = await student.PostAsJsonAsync(
            "/api/messages/conversations/start-or-get", new { courseId = course.CourseId });
        started.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversationId = (await started.Content.ReadFromJsonAsync<ConversationDto>())!.Id;

        return (course, student, conversationId);
    }

    [Fact]
    public async Task An_instructor_blocks_and_the_student_can_no_longer_send()
    {
        var (course, student, conversationId) = await SetupAsync();

        var blocked = await course.Owner.PostAsync($"/api/messages/conversations/{conversationId}/block", null);
        blocked.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var send = await student.PostAsJsonAsync(
            $"/api/messages/conversations/{conversationId}/messages", new { content = "Hello?" });
        send.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_blocked_student_cannot_unblock_themselves()
    {
        var (course, student, conversationId) = await SetupAsync();
        await course.Owner.PostAsync($"/api/messages/conversations/{conversationId}/block", null);

        var unblock = await student.PostAsync($"/api/messages/conversations/{conversationId}/unblock", null);

        unblock.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var send = await student.PostAsJsonAsync(
            $"/api/messages/conversations/{conversationId}/messages", new { content = "Let me back in?" });
        send.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_blocker_can_unblock_and_messaging_resumes()
    {
        var (course, student, conversationId) = await SetupAsync();
        await course.Owner.PostAsync($"/api/messages/conversations/{conversationId}/block", null);

        var unblocked = await course.Owner.PostAsync($"/api/messages/conversations/{conversationId}/unblock", null);
        unblocked.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var send = await student.PostAsJsonAsync(
            $"/api/messages/conversations/{conversationId}/messages", new { content = "Back online." });
        send.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Blocking_an_already_blocked_conversation_is_a_conflict()
    {
        var (course, _, conversationId) = await SetupAsync();
        await course.Owner.PostAsync($"/api/messages/conversations/{conversationId}/block", null);

        var secondBlock = await course.Owner.PostAsync($"/api/messages/conversations/{conversationId}/block", null);

        secondBlock.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task The_blocked_filter_returns_only_blocked_conversations()
    {
        var (course, _, conversationId) = await SetupAsync();
        await course.Owner.PostAsync($"/api/messages/conversations/{conversationId}/block", null);

        var blockedOnly = await course.Owner
            .GetFromJsonAsync<PagedResult<ConversationDto>>("/api/messages/conversations?isBlocked=true");
        var unblockedOnly = await course.Owner
            .GetFromJsonAsync<PagedResult<ConversationDto>>("/api/messages/conversations?isBlocked=false");

        blockedOnly!.Items.Should().ContainSingle(c => c.Id == conversationId && c.BlockedByMe);
        unblockedOnly!.Items.Should().BeEmpty();
    }

    /// <summary>Same shape as <see cref="CourseWorkspace.NewCourseAsync"/>, but the owner is a real
    /// persisted <c>User</c> row — required for <c>CourseConversation.InstructorId</c>'s FK.</summary>
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
