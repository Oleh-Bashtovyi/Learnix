using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.ForgotPassword;

namespace Learnix.Application.UnitTests.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandlerTests
{
    private readonly IPasswordResetService _passwordResetService = Substitute.For<IPasswordResetService>();
    private readonly ForgotPasswordCommandHandler _sut;

    public ForgotPasswordCommandHandlerTests()
    {
        _sut = new ForgotPasswordCommandHandler(_passwordResetService);
    }

    [Fact]
    public async Task Handle_ShouldCallInitiateResetAsyncAndReturnItsResult()
    {
        // Arrange
        var serviceResult = Result.Ok();
        _passwordResetService.InitiateResetAsync("test@example.com", Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        var command = new ForgotPasswordCommand("test@example.com");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _passwordResetService.Received(1).InitiateResetAsync("test@example.com", Arg.Any<CancellationToken>());
    }
}
