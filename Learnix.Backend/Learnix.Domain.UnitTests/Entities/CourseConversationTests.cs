using Learnix.Domain.Entities;

namespace Learnix.Domain.UnitTests.Entities;

public class CourseConversationTests
{
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid InstructorId = Guid.NewGuid();

    private static CourseConversation Conversation() =>
        CourseConversation.Create(Guid.NewGuid(), StudentId, InstructorId);

    [Fact]
    public void Create_ShouldStartUnblocked()
    {
        var conversation = Conversation();

        conversation.IsBlocked.Should().BeFalse();
        conversation.BlockedByUserId.Should().BeNull();
    }

    [Fact]
    public void Block_ShouldRecordTheBlocker()
    {
        var conversation = Conversation();

        conversation.Block(InstructorId);

        conversation.IsBlocked.Should().BeTrue();
        conversation.BlockedByUserId.Should().Be(InstructorId);
    }

    [Fact]
    public void Unblock_ShouldClearTheBlocker()
    {
        var conversation = Conversation();
        conversation.Block(StudentId);

        conversation.Unblock();

        conversation.IsBlocked.Should().BeFalse();
        conversation.BlockedByUserId.Should().BeNull();
    }
}
