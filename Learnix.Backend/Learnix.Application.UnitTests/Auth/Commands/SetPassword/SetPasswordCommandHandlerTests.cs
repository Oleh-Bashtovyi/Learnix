using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.SetPassword;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;

namespace Learnix.Application.UnitTests.Auth.Commands.SetPassword;

public class SetPasswordCommandHandlerTests
{
    private readonly ISetPasswordService _setPasswordService = Substitute.For<ISetPasswordService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly SetPasswordCommandHandler _sut;

    public SetPasswordCommandHandlerTests()
    {
        _sut = new SetPasswordCommandHandler(_setPasswordService, _currentUserService);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var command = new SetPasswordCommand("newPassword");

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
        _setPasswordService.SetPasswordAsync(userId, "newPassword", Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        var command = new SetPasswordCommand("newPassword");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _setPasswordService.Received(1).SetPasswordAsync(userId, "newPassword", Arg.Any<CancellationToken>());
    }
}
