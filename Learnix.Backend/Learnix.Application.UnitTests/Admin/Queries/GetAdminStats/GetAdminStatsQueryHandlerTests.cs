using Learnix.Application.Admin.Constants;
using Learnix.Application.Admin.Queries.GetAdminStats;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.InstructorApplications.Specifications;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Constants;

namespace Learnix.Application.UnitTests.Admin.Queries.GetAdminStats;

public class GetAdminStatsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IInstructorApplicationRepository _applicationRepository = Substitute.For<IInstructorApplicationRepository>();
    private readonly GetAdminStatsQueryHandler _sut;

    public GetAdminStatsQueryHandlerTests()
    {
        _sut = new GetAdminStatsQueryHandler(
            _currentUserService,
            _userRepository,
            _courseRepository,
            _applicationRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var query = new GetAdminStatsQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAdmin()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(false);
        var query = new GetAdminStatsQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ForbiddenError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(AdminMessages.OnlyAdminsViewStats);
    }

    [Fact]
    public async Task Handle_ShouldReturnStats_WhenUserIsAdmin()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        _userRepository.CountAsync(Arg.Any<AdminUserListCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(150);

        _courseRepository.CountAsync(Arg.Any<AdminCourseListCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(45);

        _courseRepository.CountAsync(Arg.Any<AdminCoursesByStatusCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(30);

        _applicationRepository.CountAsync(Arg.Any<PendingApplicationsCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var query = new GetAdminStatsQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalUsers.Should().Be(150);
        result.Value.TotalCourses.Should().Be(45);
        result.Value.PublishedCourses.Should().Be(30);
        result.Value.DraftCourses.Should().Be(15);
        result.Value.PendingApplications.Should().Be(5);
    }
}
