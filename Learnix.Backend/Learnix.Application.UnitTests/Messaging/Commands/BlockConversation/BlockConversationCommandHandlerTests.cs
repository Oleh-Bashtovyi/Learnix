using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Commands.BlockConversation;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Messaging.Commands.BlockConversation;

public class BlockConversationCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IConversationRepository _conversationRepository = Substitute.For<IConversationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BlockConversationCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid InstructorId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid ConversationId = Guid.NewGuid();

    public BlockConversationCommandHandlerTests()
    {
        _currentUser.UserId.Returns(InstructorId);
        _sut = new BlockConversationCommandHandler(_currentUser, _conversationRepository, _unitOfWork);
    }

    private void ConversationIs(CourseConversation? conversation) =>
        _conversationRepository
            .FirstOrDefaultAsync(
                Arg.Any<ISingleResultSpecification<CourseConversation>>(), Arg.Any<CancellationToken>())
            .Returns(conversation);

    private Task<FluentResults.Result> Act() =>
        _sut.Handle(new BlockConversationCommand(ConversationId), CancellationToken.None);

    [Fact]
    public async Task A_participant_can_block_an_unblocked_conversation()
    {
        var conversation = CourseConversation.Create(CourseId, StudentId, InstructorId);
        ConversationIs(conversation);

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        conversation.IsBlocked.Should().BeTrue();
        conversation.BlockedByUserId.Should().Be(InstructorId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_third_party_cannot_block_somebody_elses_conversation()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        ConversationIs(CourseConversation.Create(CourseId, StudentId, InstructorId));

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
    }

    [Fact]
    public async Task Blocking_an_already_blocked_conversation_does_not_reassign_the_blocker()
    {
        var conversation = CourseConversation.Create(CourseId, StudentId, InstructorId);
        conversation.Block(StudentId);
        ConversationIs(conversation);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();
        conversation.BlockedByUserId.Should().Be(StudentId);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task A_conversation_that_does_not_exist_is_not_found()
    {
        ConversationIs(null);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }
}
