using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Achievements.Commands.MarkAchievementSeen;
using Learnix.Application.Achievements.Constants;
using Learnix.Application.Achievements.Specifications;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Entities;
using NSubstitute.ReturnsExtensions;

namespace Learnix.Application.UnitTests.Achievements.Commands.MarkAchievementSeen;

public class MarkAchievementSeenCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserAchievementRepository _achievementRepo = Substitute.For<IUserAchievementRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly MarkAchievementSeenCommandHandler _sut;

    public MarkAchievementSeenCommandHandlerTests()
    {
        _sut = new MarkAchievementSeenCommandHandler(_currentUserService, _achievementRepo, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var command = new MarkAchievementSeenCommand(Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenAchievementNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _achievementRepo.FirstOrDefaultAsync(Arg.Any<UserAchievementByIdSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var command = new MarkAchievementSeenCommand(Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(AchievementMessages.AchievementNotFound);
    }

    [Fact]
    public async Task Handle_ShouldMarkAchievementSeenAndReturnOk_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievementId = Guid.NewGuid();
        var achievement = UserAchievement.Unlock(userId, "code");

        _currentUserService.UserId.Returns(userId);
        _achievementRepo.FirstOrDefaultAsync(Arg.Any<UserAchievementByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(achievement);

        var command = new MarkAchievementSeenCommand(achievementId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        achievement.Seen.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
