using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Commands.AdminPublishCourse;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute.ReturnsExtensions;

namespace Learnix.Application.UnitTests.Courses.Commands.AdminPublishCourse;

public class AdminPublishCourseCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly AdminPublishCourseCommandHandler _sut;

    public AdminPublishCourseCommandHandlerTests()
    {
        _sut = new AdminPublishCourseCommandHandler(_currentUserService, _courseRepository, _unitOfWork, _cache);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var command = new AdminPublishCourseCommand(Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

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
        var command = new AdminPublishCourseCommand(Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ForbiddenError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CourseMessages.OnlyAdminsForcePublish);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCourseNotFound()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        _courseRepository.FirstOrDefaultAsync(Arg.Any<AdminCourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var command = new AdminPublishCourseCommand(courseId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.CourseNotFound(courseId));
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCourseAlreadyPublished()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);
        course.SetCoverImage("cover.jpg");
        var section = course.AddSection("Test");
        var lesson = VideoLesson.Create(section.Id, "Video", "path.mp4");
        course.AddLesson(lesson);
        course.ToggleLessonVisibility(lesson, true);
        course.Publish();

        _courseRepository.FirstOrDefaultAsync(Arg.Any<AdminCourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var command = new AdminPublishCourseCommand(course.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ConflictError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CourseMessages.CourseAlreadyPublished);
    }

    [Fact]
    public async Task Handle_ShouldPublishCourseAndClearCache_WhenSuccessful()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);
        course.SetCoverImage("cover.jpg");
        var section = course.AddSection("Test");
        var lesson = VideoLesson.Create(section.Id, "Video", "path.mp4");
        course.AddLesson(lesson);
        course.ToggleLessonVisibility(lesson, true);

        _courseRepository.FirstOrDefaultAsync(Arg.Any<AdminCourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var command = new AdminPublishCourseCommand(course.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        course.Status.Should().Be(Learnix.Domain.Enums.CourseStatus.Published);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.Courses.ById(course.Id), Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.Courses.Featured, Arg.Any<CancellationToken>());
    }
}
