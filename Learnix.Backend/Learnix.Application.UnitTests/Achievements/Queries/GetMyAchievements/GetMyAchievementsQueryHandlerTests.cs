using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Achievements.Queries.GetMyAchievements;
using Learnix.Application.Achievements.Specifications;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Achievements.Queries.GetMyAchievements;

public class GetMyAchievementsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserAchievementRepository _achievementRepo = Substitute.For<IUserAchievementRepository>();
    private readonly IUserAchievementProgressRepository _progressRepo = Substitute.For<IUserAchievementProgressRepository>();
    private readonly GetMyAchievementsQueryHandler _sut;

    public GetMyAchievementsQueryHandlerTests()
    {
        _sut = new GetMyAchievementsQueryHandler(_currentUserService, _achievementRepo, _progressRepo);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var query = new GetMyAchievementsQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnAchievementsAndZeroProgress_WhenNoProgressExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        var achievement = UserAchievement.Unlock(userId, "code");
        _achievementRepo.ListAsync(Arg.Any<UserAchievementsByUserSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserAchievement> { achievement });

        _progressRepo.GetAsync(userId, Arg.Any<CancellationToken>()).Returns((UserAchievementProgress?)null);

        var query = new GetMyAchievementsQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Unlocked.Should().HaveCount(1);
        result.Value.Progress.LessonsCompleted.Should().Be(0);
        result.Value.Progress.CoursesCompleted.Should().Be(0);
        result.Value.Progress.DistinctCategoriesCompleted.Should().Be(0);
        result.Value.Progress.ProfileCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnAchievementsAndProgress_WhenProgressExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        var achievement = UserAchievement.Unlock(userId, "code");
        _achievementRepo.ListAsync(Arg.Any<UserAchievementsByUserSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserAchievement> { achievement });

        var progress = UserAchievementProgress.Create(userId);
        progress.SetLessonsCompleted(1);
        progress.SetCoursesCompleted(1);
        progress.SetDistinctCategoriesCompleted(3);
        progress.SetProfileCompleted(true);

        _progressRepo.GetAsync(userId, Arg.Any<CancellationToken>()).Returns(progress);

        var query = new GetMyAchievementsQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Unlocked.Should().HaveCount(1);
        result.Value.Progress.LessonsCompleted.Should().Be(1);
        result.Value.Progress.CoursesCompleted.Should().Be(1);
        result.Value.Progress.DistinctCategoriesCompleted.Should().Be(3);
        result.Value.Progress.ProfileCompleted.Should().BeTrue();
    }
}
