using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.GoogleLogin;
using Learnix.Application.Auth.Models;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Models;

namespace Learnix.Application.UnitTests.Auth.Commands.GoogleLogin;

public class GoogleLoginCommandHandlerTests
{
    private readonly IGoogleTokenValidator _googleTokenValidator = Substitute.For<IGoogleTokenValidator>();
    private readonly IUserRegistrationService _userRegistrationService = Substitute.For<IUserRegistrationService>();
    private readonly IUserAuthenticationService _userAuthenticationService = Substitute.For<IUserAuthenticationService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GoogleLoginCommandHandler _sut;

    public GoogleLoginCommandHandlerTests()
    {
        _sut = new GoogleLoginCommandHandler(
            _googleTokenValidator,
            _userRegistrationService,
            _userAuthenticationService,
            _tokenService,
            _refreshTokenRepository,
            _blobStorage,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenGoogleTokenIsInvalid()
    {
        // Arrange
        _googleTokenValidator.ValidateAsync("invalid-token", Arg.Any<CancellationToken>())
            .Returns(Result.Fail<GoogleUserInfo>("Invalid token"));

        var command = new GoogleLoginCommand("invalid-token");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Invalid token");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenFindOrCreateFails()
    {
        // Arrange
        var googleUserInfo = new GoogleUserInfo("sub", "test@test.com", true, "Test", "User");
        _googleTokenValidator.ValidateAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(Result.Ok(googleUserInfo));

        _userRegistrationService.FindOrCreateGoogleUserAsync(googleUserInfo, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<Guid>("Registration failed"));

        var command = new GoogleLoginCommand("valid-token");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Registration failed");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenAuthInfoRetrievalFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var googleUserInfo = new GoogleUserInfo("sub", "test@test.com", true, "Test", "User");

        _googleTokenValidator.ValidateAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(Result.Ok(googleUserInfo));

        _userRegistrationService.FindOrCreateGoogleUserAsync(googleUserInfo, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(userId));

        _userAuthenticationService.GetAuthenticationInfoAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<UserAuthenticationInfo>("Auth info failed"));

        var command = new GoogleLoginCommand("valid-token");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Auth info failed");
    }

    [Fact]
    public async Task Handle_ShouldGenerateTokensAndReturnResponse_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var googleUserInfo = new GoogleUserInfo("sub", "test@test.com", true, "Test", "User");
        var authInfo = new UserAuthenticationInfo(userId, "test@test.com", "Test", "User", new[] { "Student" }, true, "path/avatar.jpg");

        _googleTokenValidator.ValidateAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(Result.Ok(googleUserInfo));

        _userRegistrationService.FindOrCreateGoogleUserAsync(googleUserInfo, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(userId));

        _userAuthenticationService.GetAuthenticationInfoAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(authInfo));

        var accessTokenInfo = new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(15));
        var refreshTokenInfo = new RefreshTokenResult("refresh-token", "hash", DateTime.UtcNow.AddDays(7));

        _tokenService.GenerateAccessToken(userId, "test@test.com", "Test", "User", authInfo.Roles, true)
            .Returns(accessTokenInfo);

        _tokenService.GenerateRefreshToken()
            .Returns(refreshTokenInfo);

        _blobStorage.GetPublicUrl("path/avatar.jpg").Returns("http://example.com/avatar.jpg");

        var command = new GoogleLoginCommand("valid-token");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.AvatarUrl.Should().Be("http://example.com/avatar.jpg");

        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<Learnix.Domain.Entities.RefreshToken>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
