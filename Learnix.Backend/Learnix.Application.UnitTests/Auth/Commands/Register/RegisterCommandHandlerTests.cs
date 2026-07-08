using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.Register;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Models;
using RefreshTokenEntity = Learnix.Domain.Entities.RefreshToken;

namespace Learnix.Application.UnitTests.Auth.Commands.Register;

public class RegisterCommandHandlerTests
{
    private readonly IUserRegistrationService _registrationService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _registrationService = Substitute.For<IUserRegistrationService>();
        _tokenService = Substitute.For<ITokenService>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new RegisterCommandHandler(
            _registrationService,
            _tokenService,
            _refreshTokenRepository,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenRegistrationSucceeds_ShouldReturnLoginResponse()
    {
        // Arrange
        var command = new RegisterCommand("test@example.com", "Password123!", "Test", "User", "en");
        var userId = Guid.NewGuid();

        var registrationResult = (UserId: userId, EmailConfirmationToken: "token");

        _registrationService.RegisterAsync(
            command.Email, command.Password, command.FirstName, command.LastName, command.Language, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(registrationResult));

        _tokenService.GenerateAccessToken(
            userId, command.Email, command.FirstName, command.LastName, Arg.Any<string[]>(), false)
            .Returns(new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(15)));

        _tokenService.GenerateRefreshToken()
            .Returns(new RefreshTokenResult("refresh-token", "hash", DateTime.UtcNow.AddDays(7)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");

        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRegistrationFails_ShouldReturnFailedResult()
    {
        // Arrange
        var command = new RegisterCommand("test@example.com", "Password123!", "Test", "User", "en");

        _registrationService.RegisterAsync(
            command.Email, command.Password, command.FirstName, command.LastName, command.Language, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<(Guid, string)>("Email already in use"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "Email already in use");

        await _refreshTokenRepository.DidNotReceive().AddAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
