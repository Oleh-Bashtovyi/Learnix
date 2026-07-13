using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.ResetPassword;

namespace Learnix.Application.UnitTests.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandlerTests
{
    private readonly IPasswordResetService _passwordResetService = Substitute.For<IPasswordResetService>();
    private readonly ResetPasswordCommandHandler _sut;

    public ResetPasswordCommandHandlerTests()
    {
        _sut = new ResetPasswordCommandHandler(_passwordResetService);
    }

    [Fact]
    public async Task Handle_ShouldCallResetPasswordAsyncAndReturnItsResult()
    {
        // Arrange
        var serviceResult = Result.Ok();
        _passwordResetService.ResetPasswordAsync("test@example.com", "validToken", "newPass", Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        var command = new ResetPasswordCommand("test@example.com", "validToken", "newPass");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _passwordResetService.Received(1).ResetPasswordAsync("test@example.com", "validToken", "newPass", Arg.Any<CancellationToken>());
    }
}
