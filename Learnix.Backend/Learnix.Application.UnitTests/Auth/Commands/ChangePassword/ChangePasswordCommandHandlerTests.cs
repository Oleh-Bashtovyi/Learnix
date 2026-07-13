using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.ChangePassword;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;

namespace Learnix.Application.UnitTests.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandlerTests
{
    private readonly IChangePasswordService _changePasswordService = Substitute.For<IChangePasswordService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ChangePasswordCommandHandler _sut;

    public ChangePasswordCommandHandlerTests()
    {
        _sut = new ChangePasswordCommandHandler(_changePasswordService, _currentUserService);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var command = new ChangePasswordCommand("oldPass", "newPass");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnServiceResult_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        var serviceResult = Result.Ok();
        _changePasswordService.ChangePasswordAsync(userId, "oldPass", "newPass", Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        var command = new ChangePasswordCommand("oldPass", "newPass");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _changePasswordService.Received(1).ChangePasswordAsync(userId, "oldPass", "newPass", Arg.Any<CancellationToken>());
    }
}
