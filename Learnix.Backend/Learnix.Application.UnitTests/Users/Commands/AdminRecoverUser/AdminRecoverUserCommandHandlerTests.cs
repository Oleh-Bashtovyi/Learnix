using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Commands.AdminRecoverUser;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;

namespace Learnix.Application.UnitTests.Users.Commands.AdminRecoverUser;

public class AdminRecoverUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AdminRecoverUserCommandHandler _sut;

    private static readonly Guid TargetId = Guid.NewGuid();

    public AdminRecoverUserCommandHandlerTests()
    {
        _sut = new AdminRecoverUserCommandHandler(_userRepository, _unitOfWork);
    }

    private void TargetIs(User? user) =>
        _userRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns(user);

    private static User DeletedUser()
    {
        var user = new User("back@learnix.dev", "Coming", "Back");
        user.SoftDelete();
        user.ClearDomainEvents();
        return user;
    }

    private Task<FluentResults.Result> Act() =>
        _sut.Handle(new AdminRecoverUserCommand(TargetId), CancellationToken.None);

    [Fact]
    public async Task Recovering_undeletes_the_account_and_raises_the_event_that_emails_the_user()
    {
        var user = DeletedUser();
        TargetIs(user);

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        user.IsDeleted.Should().BeFalse();
        user.DeletedAt.Should().BeNull();
        user.DomainEvents.Should().ContainSingle(e => e is UserRecoveredDomainEvent);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Recovering a live account would email somebody about an undoing that never happened.</summary>
    [Fact]
    public async Task Recovering_an_account_that_was_never_deleted_is_a_conflict()
    {
        var user = new User("here@learnix.dev", "Still", "Here");
        TargetIs(user);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();
        user.DomainEvents.Should().BeEmpty();
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task A_user_who_does_not_exist_is_not_found()
    {
        TargetIs(null);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }
}
