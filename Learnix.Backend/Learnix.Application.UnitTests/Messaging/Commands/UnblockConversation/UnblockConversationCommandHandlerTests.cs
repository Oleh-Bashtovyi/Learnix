using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Commands.UnblockConversation;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Messaging.Commands.UnblockConversation;

public class UnblockConversationCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IConversationRepository _conversationRepository = Substitute.For<IConversationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UnblockConversationCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid InstructorId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid ConversationId = Guid.NewGuid();

    public UnblockConversationCommandHandlerTests()
    {
        _sut = new UnblockConversationCommandHandler(_currentUser, _conversationRepository, _unitOfWork);
    }

    private void ConversationIs(CourseConversation? conversation) =>
        _conversationRepository
            .FirstOrDefaultAsync(
                Arg.Any<ISingleResultSpecification<CourseConversation>>(), Arg.Any<CancellationToken>())
            .Returns(conversation);

    private Task<FluentResults.Result> Act() =>
        _sut.Handle(new UnblockConversationCommand(ConversationId), CancellationToken.None);

    [Fact]
    public async Task The_blocker_can_unblock_their_own_block()
    {
        var conversation = CourseConversation.Create(CourseId, StudentId, InstructorId);
        conversation.Block(InstructorId);
        ConversationIs(conversation);
        _currentUser.UserId.Returns(InstructorId);

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        conversation.IsBlocked.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>The scenario the feature exists for: the blocked side cannot lift its own block.</summary>
    [Fact]
    public async Task Only_the_user_who_blocked_can_unblock()
    {
        var conversation = CourseConversation.Create(CourseId, StudentId, InstructorId);
        conversation.Block(InstructorId);
        ConversationIs(conversation);
        _currentUser.UserId.Returns(StudentId);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        conversation.IsBlocked.Should().BeTrue();
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Unblocking_a_conversation_that_is_not_blocked_is_a_conflict()
    {
        ConversationIs(CourseConversation.Create(CourseId, StudentId, InstructorId));
        _currentUser.UserId.Returns(InstructorId);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();
    }

    [Fact]
    public async Task A_third_party_cannot_unblock_somebody_elses_conversation()
    {
        var conversation = CourseConversation.Create(CourseId, StudentId, InstructorId);
        conversation.Block(InstructorId);
        ConversationIs(conversation);
        _currentUser.UserId.Returns(Guid.NewGuid());

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
    }

    [Fact]
    public async Task A_conversation_that_does_not_exist_is_not_found()
    {
        ConversationIs(null);
        _currentUser.UserId.Returns(InstructorId);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }
}
