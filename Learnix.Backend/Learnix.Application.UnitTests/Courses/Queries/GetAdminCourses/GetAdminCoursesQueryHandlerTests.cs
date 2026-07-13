using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Queries.GetAdminCourses;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Courses.Queries.GetAdminCourses;

public class GetAdminCoursesQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IBlobStorageService _blobStorageService = Substitute.For<IBlobStorageService>();
    private readonly GetAdminCoursesQueryHandler _sut;

    public GetAdminCoursesQueryHandlerTests()
    {
        _sut = new GetAdminCoursesQueryHandler(_currentUserService, _courseRepository, _blobStorageService);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var query = new GetAdminCoursesQuery(null, 0, 10, null, false);

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
        var query = new GetAdminCoursesQuery(null, 0, 10, null, false);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ForbiddenError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CourseMessages.OnlyAdminsViewAllCourses);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPaginatedResult_WhenTotalCountIsZero()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        _courseRepository.CountAsync(Arg.Any<CourseListCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var query = new GetAdminCoursesQuery(null, 0, 10, null, false);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);

        await _courseRepository.DidNotReceive().ListAsync(Arg.Any<CourseListSpecification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnCoursesWithoutDeleted_WhenIncludeDeletedIsFalse()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        _courseRepository.CountAsync(Arg.Any<CourseListCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);
        course.SetCoverImage("path/to/cover.jpg");

        _courseRepository.ListAsync(Arg.Any<CourseListSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<Course> { course });

        _blobStorageService.GetPublicUrl("path/to/cover.jpg").Returns("http://storage.com/cover.jpg");

        var query = new GetAdminCoursesQuery(null, 0, 10, null, false);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);

        var dto = result.Value.Items[0];
        dto.Id.Should().Be(course.Id);
        dto.Title.Should().Be("Title");
        dto.CoverImageUrl.Should().Be("http://storage.com/cover.jpg");
        dto.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnCoursesIncludingDeleted_WhenIncludeDeletedIsTrue()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        _courseRepository.CountAsync(Arg.Any<AdminCourseListCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);
        course.Delete();

        _courseRepository.ListAsync(Arg.Any<AdminCourseListSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<Course> { course });

        var query = new GetAdminCoursesQuery(null, 0, 10, null, true);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);

        var dto = result.Value.Items[0];
        dto.Id.Should().Be(course.Id);
        dto.Title.Should().Be("Title");
        dto.IsDeleted.Should().BeTrue();
    }
}
