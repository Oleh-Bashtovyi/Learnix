using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.InstructorApplications.Constants;
using Learnix.Application.InstructorApplications.Queries.GetPendingApplications;
using Learnix.Application.InstructorApplications.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.InstructorApplications.Queries.GetPendingApplications;

public class GetPendingApplicationsQueryHandlerTests
{
    private readonly IInstructorApplicationRepository _repo = Substitute.For<IInstructorApplicationRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetPendingApplicationsQueryHandler _sut;

    public GetPendingApplicationsQueryHandlerTests()
    {
        _sut = new GetPendingApplicationsQueryHandler(_repo, _currentUserService);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var query = new GetPendingApplicationsQuery(0, 10);

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
        var query = new GetPendingApplicationsQuery(0, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ForbiddenError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(InstructorApplicationMessages.OnlyAdminsViewPending);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPaginatedResult_WhenTotalCountIsZero()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        _repo.CountAsync(Arg.Any<PendingApplicationsCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var query = new GetPendingApplicationsQuery(0, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);

        await _repo.DidNotReceive().ListAsync(Arg.Any<PendingApplicationsSpecification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedApplications_WhenSuccessful()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _currentUserService.UserId.Returns(adminId);
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        _repo.CountAsync(Arg.Any<PendingApplicationsCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var applicantId = Guid.NewGuid();
        var application = InstructorApplication.Create(applicantId, "Motivation", "http://portfolio.com");
        var user = new User("applicant@test.com", "App", "Licant");
        typeof(User).GetProperty("Id")?.SetValue(user, applicantId);
        typeof(InstructorApplication).GetProperty(nameof(InstructorApplication.User))?.SetValue(application, user);

        _repo.ListAsync(Arg.Any<PendingApplicationsSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<InstructorApplication> { application });

        var query = new GetPendingApplicationsQuery(0, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);

        var dto = result.Value.Items[0];
        dto.Id.Should().Be(application.Id);
        dto.UserId.Should().Be(applicantId);
        dto.FirstName.Should().Be("App");
        dto.LastName.Should().Be("Licant");
        dto.Email.Should().Be("applicant@test.com");
        dto.MotivationText.Should().Be("Motivation");
        dto.PortfolioUrl.Should().Be("http://portfolio.com");
    }
}
