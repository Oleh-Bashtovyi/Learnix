using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Commands.AdminDeleteUser;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;

namespace Learnix.Application.UnitTests.Users.Commands.AdminDeleteUser;

public class AdminDeleteUserCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AdminDeleteUserCommandHandler _sut;

    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();

    public AdminDeleteUserCommandHandlerTests()
    {
        _currentUser.UserId.Returns(AdminId);
        _sut = new AdminDeleteUserCommandHandler(_currentUser, _userRepository, _unitOfWork);
    }

    private void TargetIs(User? user) =>
        _userRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns(user);

    private Task<FluentResults.Result> Act(Guid? userId = null) =>
        _sut.Handle(new AdminDeleteUserCommand(userId ?? TargetId), CancellationToken.None);

    /// <summary>
    /// Deletion is soft, and the event it raises is what sends the goodbye email вЂ” the one place the user is
    /// told the account survives and can still be restored.
    /// </summary>
    [Fact]
    public async Task Deleting_flags_the_account_and_raises_the_event_that_emails_the_user()
    {
        var user = new User("leaving@learnix.dev", "Leaving", "User");
        TargetIs(user);

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        user.IsDeleted.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle(e => e is UserDeletedDomainEvent);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_admin_cannot_delete_themselves()
    {
        var result = await Act(AdminId);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();
        await _userRepository.DidNotReceiveWithAnyArgs()
            .FirstOrDefaultAsync(default(ISingleResultSpecification<User>)!, default);
    }

    /// <summary>A second delete must not send a second goodbye email.</summary>
    [Fact]
    public async Task Deleting_an_already_deleted_account_is_a_conflict()
    {
        var user = new User("gone@learnix.dev", "Gone", "User");
        user.SoftDelete();
        user.ClearDomainEvents();
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
