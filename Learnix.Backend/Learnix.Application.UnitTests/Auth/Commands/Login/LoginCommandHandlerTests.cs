using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.Login;
using Learnix.Application.Auth.Models;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Models;
using RefreshTokenEntity = Learnix.Domain.Entities.RefreshToken;

namespace Learnix.Application.UnitTests.Auth.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly IUserAuthenticationService _authService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _authService = Substitute.For<IUserAuthenticationService>();
        _tokenService = Substitute.For<ITokenService>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _blobStorage = Substitute.For<IBlobStorageService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new LoginCommandHandler(
            _authService,
            _tokenService,
            _refreshTokenRepository,
            _blobStorage,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ShouldReturnLoginResponse()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123!");
        var userId = Guid.NewGuid();

        var authInfo = new UserAuthenticationInfo(
            userId,
            command.Email,
            "Test",
            "User",
            new[] { "Student" },
            true,
            "avatars/1.jpg");

        _authService.ValidateCredentialsAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(authInfo));

        _tokenService.GenerateAccessToken(
            userId, command.Email, "Test", "User", Arg.Is<IReadOnlyList<string>>(r => r.Contains("Student")), true)
            .Returns(new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(15)));

        _tokenService.GenerateRefreshToken()
            .Returns(new RefreshTokenResult("refresh-token", "hash", DateTime.UtcNow.AddDays(7)));

        _blobStorage.GetPublicUrl("avatars/1.jpg")
            .Returns("https://storage.com/avatars/1.jpg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.AvatarUrl.Should().Be("https://storage.com/avatars/1.jpg");

        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreInvalid_ShouldReturnFailedResult()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "WrongPassword!");

        _authService.ValidateCredentialsAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<UserAuthenticationInfo>("Invalid credentials"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "Invalid credentials");

        await _refreshTokenRepository.DidNotReceive().AddAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
