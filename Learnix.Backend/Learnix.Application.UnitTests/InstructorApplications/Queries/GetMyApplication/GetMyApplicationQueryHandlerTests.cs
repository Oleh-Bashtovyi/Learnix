using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.InstructorApplications.Queries.GetMyApplication;
using Learnix.Application.InstructorApplications.Specifications;
using Learnix.Domain.Entities;
using NSubstitute.ReturnsExtensions;

namespace Learnix.Application.UnitTests.InstructorApplications.Queries.GetMyApplication;

public class GetMyApplicationQueryHandlerTests
{
    private readonly IInstructorApplicationRepository _repo = Substitute.For<IInstructorApplicationRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetMyApplicationQueryHandler _sut;

    public GetMyApplicationQueryHandlerTests()
    {
        _sut = new GetMyApplicationQueryHandler(_repo, _currentUserService);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var query = new GetMyApplicationQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenNoApplicationExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        _repo.FirstOrDefaultAsync(Arg.Any<ApplicationByUserIdSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var query = new GetMyApplicationQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnApplicationResponse_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        var application = InstructorApplication.Create(userId, "Motivation", "http://portfolio.com");

        _repo.FirstOrDefaultAsync(Arg.Any<ApplicationByUserIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(application);

        var query = new GetMyApplicationQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(application.Id);
        result.Value.Status.Should().Be("Pending");
        result.Value.MotivationText.Should().Be("Motivation");
        result.Value.PortfolioUrl.Should().Be("http://portfolio.com");
    }
}
