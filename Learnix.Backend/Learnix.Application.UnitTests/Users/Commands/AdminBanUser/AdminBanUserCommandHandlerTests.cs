using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Commands.AdminBanUser;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Users.Commands.AdminBanUser;

public class AdminBanUserCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AdminBanUserCommandHandler _sut;

    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();

    public AdminBanUserCommandHandlerTests()
    {
        _currentUser.UserId.Returns(AdminId);
        _sut = new AdminBanUserCommandHandler(_currentUser, _userRepository, _unitOfWork);
    }

    private void TargetIs(User? user) =>
        _userRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns(user);

    private Task<FluentResults.Result> Act(Guid? userId = null) =>
        _sut.Handle(new AdminBanUserCommand(userId ?? TargetId), CancellationToken.None);

    [Fact]
    public async Task Banning_locks_the_account_out_indefinitely()
    {
        var user = new User("spammer@learnix.dev", "Spam", "Merchant");
        TargetIs(user);

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        user.LockoutEnabled.Should().BeTrue();
        user.LockoutEnd.Should().Be(DateTimeOffset.MaxValue);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>An admin who bans themselves locks the platform's last door from the inside.</summary>
    [Fact]
    public async Task An_admin_cannot_ban_themselves()
    {
        var result = await Act(AdminId);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();
        await _userRepository.DidNotReceiveWithAnyArgs()
            .FirstOrDefaultAsync(default(ISingleResultSpecification<User>)!, default);
    }

    [Fact]
    public async Task Banning_an_already_banned_user_is_a_conflict()
    {
        var user = new User("spammer@learnix.dev", "Spam", "Merchant");
        user.Ban();
        TargetIs(user);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();
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
