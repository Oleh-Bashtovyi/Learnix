using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Queries.GetMyEnrollments;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Enrollments.Queries.GetMyEnrollments;

public class GetMyEnrollmentsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly IBlobStorageService _blobStorageService = Substitute.For<IBlobStorageService>();
    private readonly GetMyEnrollmentsQueryHandler _sut;

    public GetMyEnrollmentsQueryHandlerTests()
    {
        _sut = new GetMyEnrollmentsQueryHandler(_currentUserService, _enrollmentRepository, _blobStorageService);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var query = new GetMyEnrollmentsQuery(0, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPaginatedResult_WhenTotalCountIsZero()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _currentUserService.UserId.Returns(studentId);

        _enrollmentRepository.CountAsync(Arg.Any<MyEnrollmentsCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var query = new GetMyEnrollmentsQuery(0, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);

        await _enrollmentRepository.DidNotReceive().ListAsync(Arg.Any<MyEnrollmentsSpecification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedEnrollments_WhenSuccessful()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _currentUserService.UserId.Returns(studentId);

        _enrollmentRepository.CountAsync(Arg.Any<MyEnrollmentsCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);
        course.SetCoverImage("path/to/cover.jpg");
        var enrollment = Enrollment.Create(course.Id, studentId, 0m);

        // EF Core materializes navigation property
        typeof(Enrollment).GetProperty(nameof(Enrollment.Course))?.SetValue(enrollment, course);

        _enrollmentRepository.ListAsync(Arg.Any<MyEnrollmentsSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<Enrollment> { enrollment });

        _blobStorageService.GetPublicUrl("path/to/cover.jpg").Returns("http://storage.com/cover.jpg");

        var query = new GetMyEnrollmentsQuery(0, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);

        var dto = result.Value.Items[0];
        dto.EnrollmentId.Should().Be(enrollment.Id);
        dto.CourseId.Should().Be(course.Id);
        dto.CourseTitle.Should().Be("Title");
        dto.CoverImageUrl.Should().Be("http://storage.com/cover.jpg");
    }
}
