using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Queries.GetCourseForEditById;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;
using NSubstitute.ReturnsExtensions;

namespace Learnix.Application.UnitTests.Courses.Queries.GetCourseForEditById;

public class GetCourseForEditByIdQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IBlobStorageService _blobStorageService = Substitute.For<IBlobStorageService>();
    private readonly GetCourseForEditByIdQueryHandler _sut;

    public GetCourseForEditByIdQueryHandlerTests()
    {
        _sut = new GetCourseForEditByIdQueryHandler(_currentUserService, _courseRepository, _blobStorageService);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var query = new GetCourseForEditByIdQuery(Guid.NewGuid());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCourseNotFound()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _courseRepository.FirstOrDefaultAsync(Arg.Any<CourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var query = new GetCourseForEditByIdQuery(courseId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.CourseNotFound(courseId));
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotOwnerOrAdmin()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        _currentUserService.UserId.Returns(Guid.NewGuid());

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);

        _courseRepository.FirstOrDefaultAsync(Arg.Any<CourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var query = new GetCourseForEditByIdQuery(courseId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ForbiddenError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CourseMessages.NotAllowedToViewCourse);
    }

    [Fact]
    public async Task Handle_ShouldReturnCourseForEdit_WhenUserIsOwner()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        _currentUserService.UserId.Returns(instructorId);

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(instructorId, category.Id, "Title", "Desc", 0m);
        course.SetCoverImage("path/to/cover.jpg");
        var section = course.AddSection("Section 1");

        var videoLesson = VideoLesson.Create(section.Id, "Video 1", "path/to/video.mp4");
        course.AddLesson(videoLesson);

        var postLesson = PostLesson.Create(section.Id, "Post 1", "Content");
        course.AddLesson(postLesson);

        _courseRepository.FirstOrDefaultAsync(Arg.Any<CourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        _blobStorageService.GetPublicUrl("path/to/cover.jpg").Returns("http://storage.com/cover.jpg");
        _blobStorageService.GenerateReadUrl("path/to/video.mp4", Arg.Any<TimeSpan>()).Returns("http://storage.com/video.mp4");

        var query = new GetCourseForEditByIdQuery(course.Id);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dto = result.Value;
        dto.Id.Should().Be(course.Id);
        dto.InstructorId.Should().Be(instructorId);
        dto.Title.Should().Be("Title");
        dto.CoverImageUrl.Should().Be("http://storage.com/cover.jpg");

        dto.Sections.Should().HaveCount(1);
        dto.Sections[0].Lessons.Should().HaveCount(2);

        var videoDto = dto.Sections[0].Lessons.First(l => l.Id == videoLesson.Id);
        videoDto.LessonType.Should().Be("Video");
        videoDto.VideoUrl.Should().Be("http://storage.com/video.mp4");

        var postDto = dto.Sections[0].Lessons.First(l => l.Id == postLesson.Id);
        postDto.LessonType.Should().Be("Post");
        postDto.Content.Should().Be("Content");
    }
}
