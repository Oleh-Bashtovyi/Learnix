using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.Logout;
using Learnix.Application.Auth.Specifications;
using Learnix.Application.Common.Abstractions.Persistence;
using RefreshTokenEntity = Learnix.Domain.Entities.RefreshToken;

namespace Learnix.Application.UnitTests.Auth.Commands.Logout;

public class LogoutCommandHandlerTests
{
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _tokenService = Substitute.For<ITokenService>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new LogoutCommandHandler(
            _tokenService,
            _refreshTokenRepository,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenIsEmpty_ShouldReturnOkWithoutRevoking()
    {
        // Arrange
        var command = new LogoutCommand(string.Empty);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _tokenService.DidNotReceive().HashRefreshToken(Arg.Any<string>());
        await _refreshTokenRepository.DidNotReceive().FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenIsFoundAndNotRevoked_ShouldRevokeAndReturnOk()
    {
        // Arrange
        var tokenString = "valid-token";
        var command = new LogoutCommand(tokenString);
        var hashedToken = "hashed-token";

        var refreshTokenEntity = new RefreshTokenEntity(Guid.NewGuid(), hashedToken, DateTime.UtcNow.AddDays(1));

        _tokenService.HashRefreshToken(tokenString).Returns(hashedToken);
        _refreshTokenRepository.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>())
            .Returns(refreshTokenEntity);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        refreshTokenEntity.IsRevoked.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNotFoundOrAlreadyRevoked_ShouldReturnOkWithoutSaving()
    {
        // Arrange
        var tokenString = "invalid-token";
        var command = new LogoutCommand(tokenString);
        var hashedToken = "hashed-token";

        _tokenService.HashRefreshToken(tokenString).Returns(hashedToken);
        _refreshTokenRepository.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>())
            .Returns((RefreshTokenEntity?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
