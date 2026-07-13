using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Commands.PublishCourse;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.UnitTests.Courses.Commands.PublishCourse;

public class PublishCourseCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly PublishCourseCommandHandler _sut;

    public PublishCourseCommandHandlerTests()
    {
        _sut = new PublishCourseCommandHandler(_currentUserService, _courseRepository, _unitOfWork, _cache);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var command = new PublishCourseCommand(Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCoverImageIsMissing()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        _currentUserService.UserId.Returns(instructorId);

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(instructorId, category.Id, "Title", "Desc", 0m);

        _courseRepository.FirstOrDefaultAsync(Arg.Any<CourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var command = new PublishCourseCommand(course.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ConflictError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CourseMessages.CannotPublishNoCoverImage);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenNoSectionsExist()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        _currentUserService.UserId.Returns(instructorId);

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(instructorId, category.Id, "Title", "Desc", 0m);
        course.SetCoverImage("path/to/cover.jpg");

        _courseRepository.FirstOrDefaultAsync(Arg.Any<CourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var command = new PublishCourseCommand(course.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ConflictError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CourseMessages.CannotPublishNoSection);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenNoLessonsExist()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        _currentUserService.UserId.Returns(instructorId);

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(instructorId, category.Id, "Title", "Desc", 0m);
        course.SetCoverImage("path/to/cover.jpg");
        course.AddSection("Section 1");

        _courseRepository.FirstOrDefaultAsync(Arg.Any<CourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var command = new PublishCourseCommand(course.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ConflictError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CourseMessages.CannotPublishNoLesson);
    }

    [Fact]
    public async Task Handle_ShouldPublishCourseAndClearCache_WhenSuccessful()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        _currentUserService.UserId.Returns(instructorId);

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(instructorId, category.Id, "Title", "Desc", 0m);
        course.SetCoverImage("path/to/cover.jpg");
        var section = course.AddSection("Section 1");

        var lesson = VideoLesson.Create(section.Id, "Lesson 1", "path/to/video.mp4");
        course.AddLesson(lesson);
        course.ToggleLessonVisibility(lesson, true);

        _courseRepository.FirstOrDefaultAsync(Arg.Any<CourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var command = new PublishCourseCommand(course.Id);

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
