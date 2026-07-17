using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Commands.AdminRecoverCourse;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute.ReturnsExtensions;

namespace Learnix.Application.UnitTests.Courses.Commands.AdminRecoverCourse;

public class AdminRecoverCourseCommandHandlerTests
{
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly AdminRecoverCourseCommandHandler _sut;

    public AdminRecoverCourseCommandHandlerTests()
    {
        _sut = new AdminRecoverCourseCommandHandler(_courseRepository, _unitOfWork, _cache);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCourseNotFound()
    {
        // Arrange
        var courseId = Guid.NewGuid();

        _courseRepository.FirstOrDefaultAsync(Arg.Any<AdminCourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var command = new AdminRecoverCourseCommand(courseId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.CourseNotFound(courseId));
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCourseNotDeleted()
    {
        // Arrange

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);

        _courseRepository.FirstOrDefaultAsync(Arg.Any<AdminCourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var command = new AdminRecoverCourseCommand(course.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ConflictError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CourseMessages.CourseNotDeleted);
    }

    [Fact]
    public async Task Handle_ShouldRecoverCourseAndClearCache_WhenSuccessful()
    {
        // Arrange

        var category = Category.CreateSystem("Cat", "cat");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);
        course.Delete();

        _courseRepository.FirstOrDefaultAsync(Arg.Any<AdminCourseByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(course);

        var command = new AdminRecoverCourseCommand(course.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        course.IsDeleted.Should().BeFalse();

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.Courses.ById(course.Id), Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.Courses.Featured, Arg.Any<CancellationToken>());
    }
}
