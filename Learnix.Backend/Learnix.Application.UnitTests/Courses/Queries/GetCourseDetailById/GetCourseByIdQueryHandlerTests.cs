using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Queries.GetCourseById;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Entities;
using NSubstitute.ReturnsExtensions;

namespace Learnix.Application.UnitTests.Courses.Queries.GetCourseDetailById;

public class GetCourseByIdQueryHandlerTests
{
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IBlobStorageService _blobStorageService = Substitute.For<IBlobStorageService>();
    private readonly GetCourseByIdQueryHandler _sut;

    public GetCourseByIdQueryHandlerTests()
    {
        _sut = new GetCourseByIdQueryHandler(_courseRepository, _userRepository, _blobStorageService);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCourseNotFound()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        _courseRepository.FirstOrDefaultAsync(Arg.Any<CourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var query = new GetCourseByIdQuery(courseId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.CourseNotFound(courseId));
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCourseIsNotPublished()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);

        _courseRepository.FirstOrDefaultAsync(Arg.Any<CourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var query = new GetCourseByIdQuery(courseId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.CourseNotFound(courseId));
    }

    [Fact]
    public async Task Handle_ShouldReturnCourseDetail_WhenCourseIsPublished()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(instructorId, category.Id, "Title", "Desc", 0m);
        course.SetCoverImage("path/to/cover.jpg");
        var section = course.AddSection("Section 1");
        var lesson = VideoLesson.Create(section.Id, "Lesson 1", "path/to/video.mp4");
        course.AddLesson(lesson);
        course.ToggleLessonVisibility(lesson, true);
        course.Publish();

        _courseRepository.FirstOrDefaultAsync(Arg.Any<CourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var instructor = new User("john@test.com", "John", "Doe");
        _userRepository.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(instructor);

        _blobStorageService.GetPublicUrl("path/to/cover.jpg").Returns("http://storage.com/cover.jpg");

        var query = new GetCourseByIdQuery(course.Id);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dto = result.Value;
        dto.Id.Should().Be(course.Id);
        dto.InstructorId.Should().Be(instructorId);
        dto.Title.Should().Be("Title");
        dto.CoverImageUrl.Should().Be("http://storage.com/cover.jpg");
        dto.InstructorFullName.Should().Be("John Doe");

        dto.Sections.Should().HaveCount(1);
        dto.Sections[0].Lessons.Should().HaveCount(1);
        dto.Sections[0].Lessons[0].Title.Should().Be("Lesson 1");
    }
}
