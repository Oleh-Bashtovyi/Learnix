using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Hubs;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Commands.SendMessage;
using Learnix.Application.Users.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Messaging.Commands.SendMessage;

public class SendMessageCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IConversationRepository _conversationRepository = Substitute.For<IConversationRepository>();
    private readonly IMessageRepository _messageRepository = Substitute.For<IMessageRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IChatNotifier _chatNotifier = Substitute.For<IChatNotifier>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SendMessageCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid InstructorId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid ConversationId = Guid.NewGuid();

    public SendMessageCommandHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);
        _sut = new SendMessageCommandHandler(
            _currentUser, _conversationRepository, _messageRepository, _userRepository,
            _chatNotifier, _unitOfWork);
    }

    private void ConversationIs(CourseConversation? conversation) =>
        _conversationRepository
            .FirstOrDefaultAsync(
                Arg.Any<ISingleResultSpecification<CourseConversation>>(), Arg.Any<CancellationToken>())
            .Returns(conversation);

    private void SenderIs(User? user) =>
        _userRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns(user);

    private Task<FluentResults.Result<SendMessageResponse>> Act(string content = "Hello") =>
        _sut.Handle(new SendMessageCommand(ConversationId, content), CancellationToken.None);

    [Fact]
    public async Task The_student_writes_and_the_instructor_is_the_one_notified()
    {
        // Arrange
        ConversationIs(CourseConversation.Create(CourseId, StudentId, InstructorId));
        SenderIs(new User("student@learnix.dev", "Dev", "Student"));

        // Act
        var result = await Act("Where do I start?");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("Where do I start?");
        result.Value.SenderName.Should().Be("Dev Student");

        await _messageRepository.Received(1).AddAsync(Arg.Any<CourseMessage>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        await _chatNotifier.Received(1).NotifyNewMessageAsync(
            InstructorId, Arg.Any<NewMessageNotification>(), Arg.Any<CancellationToken>());
        await _chatNotifier.Received(1).NotifyUnreadCountChangedAsync(
            InstructorId, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_instructor_writes_and_the_student_is_the_one_notified()
    {
        // Arrange
        _currentUser.UserId.Returns(InstructorId);
        ConversationIs(CourseConversation.Create(CourseId, StudentId, InstructorId));
        SenderIs(new User("instructor@learnix.dev", "Dev", "Instructor"));

        // Act
        var result = await Act("Start with section one.");

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _chatNotifier.Received(1).NotifyNewMessageAsync(
            StudentId, Arg.Any<NewMessageNotification>(), Arg.Any<CancellationToken>());
        await _chatNotifier.Received(1).NotifyUnreadCountChangedAsync(
            StudentId, 1, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A conversation belongs to exactly two people. Anyone else holding its id is a stranger, however
    /// they came by it.
    /// </summary>
    [Fact]
    public async Task A_third_party_cannot_write_into_somebody_elses_conversation()
    {
        // Arrange
        _currentUser.UserId.Returns(Guid.NewGuid());
        ConversationIs(CourseConversation.Create(CourseId, StudentId, InstructorId));

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _messageRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _chatNotifier.DidNotReceiveWithAnyArgs().NotifyNewMessageAsync(default, default!, default);
    }

    [Fact]
    public async Task A_blocked_conversation_rejects_new_messages_from_either_side()
    {
        // Arrange
        var conversation = CourseConversation.Create(CourseId, StudentId, InstructorId);
        conversation.Block(InstructorId);
        ConversationIs(conversation);

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _messageRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task A_conversation_that_does_not_exist_is_not_found()
    {
        // Arrange
        ConversationIs(null);

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Nothing_is_written_when_the_message_cannot_be_attributed_to_a_sender()
    {
        // Arrange
        ConversationIs(CourseConversation.Create(CourseId, StudentId, InstructorId));
        SenderIs(null);

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
        await _messageRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
