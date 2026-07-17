using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.InstructorApplications.Commands.ApproveApplication;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.UnitTests.InstructorApplications.Commands.ApproveApplication;

public class ApproveApplicationCommandHandlerTests
{
    private readonly IInstructorApplicationRepository _repo = Substitute.For<IInstructorApplicationRepository>();
    private readonly IUserRoleService _roleService = Substitute.For<IUserRoleService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly ApproveApplicationCommandHandler _sut;

    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid ApplicantId = Guid.NewGuid();

    public ApproveApplicationCommandHandlerTests()
    {
        _sut = new ApproveApplicationCommandHandler(_repo, _roleService, _unitOfWork, _currentUser);

        // Default: an authenticated admin acting on a pending application
        _currentUser.UserId.Returns(AdminId);
        StubApplication(PendingApplication());
    }

    // Authorisation

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnAuthenticationError()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act
        var result = await _sut.Handle(new ApproveApplicationCommand(Guid.NewGuid()), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
    }

    // State guards

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        StubApplication(null);

        // Act
        var result = await _sut.Handle(new ApproveApplicationCommand(Guid.NewGuid()), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Handle_WhenApplicationWasAlreadyApproved_ShouldReturnConflictAndNotReassignTheRole()
    {
        // Arrange вЂ” double-clicking Approve must not grant the role twice
        var application = PendingApplication();
        application.Approve(AdminId);
        StubApplication(application);

        // Act
        var result = await _sut.Handle(new ApproveApplicationCommand(Guid.NewGuid()), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictError>();

        await _roleService.DidNotReceive()
            .AssignRoleAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenApplicationWasAlreadyRejected_ShouldReturnConflict()
    {
        // Arrange
        var application = PendingApplication();
        application.Reject(AdminId, "Not enough experience.");
        StubApplication(application);

        // Act
        var result = await _sut.Handle(new ApproveApplicationCommand(Guid.NewGuid()), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictError>();
    }

    // Approval

    [Fact]
    public async Task Handle_WhenApplicationIsPending_ShouldGrantTheInstructorRoleToTheApplicantNotTheAdmin()
    {
        // Arrange
        var application = PendingApplication();
        StubApplication(application);

        // Act
        var result = await _sut.Handle(new ApproveApplicationCommand(Guid.NewGuid()), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _roleService.Received(1)
            .AssignRoleAsync(ApplicantId, Roles.Instructor, Arg.Any<CancellationToken>());
        await _roleService.DidNotReceive()
            .AssignRoleAsync(AdminId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenApplicationIsPending_ShouldRecordWhoApprovedIt()
    {
        // Arrange
        var application = PendingApplication();
        StubApplication(application);

        // Act
        await _sut.Handle(new ApproveApplicationCommand(Guid.NewGuid()), default);

        // Assert
        application.Status.Should().Be(ApplicationStatus.Approved);
        application.ReviewedByAdminId.Should().Be(AdminId);
        application.ReviewedAt.Should().NotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Fixtures

    private static InstructorApplication PendingApplication() =>
        InstructorApplication.Create(ApplicantId, "I want to teach.", portfolioUrl: null);

    private void StubApplication(InstructorApplication? application) =>
        _repo
            .FirstOrDefaultAsync(Arg.Any<ISpecification<InstructorApplication>>(), Arg.Any<CancellationToken>())
            .Returns(application);
}
