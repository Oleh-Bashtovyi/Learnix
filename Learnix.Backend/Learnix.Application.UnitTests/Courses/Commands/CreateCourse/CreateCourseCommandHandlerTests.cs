using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Commands.CreateCourse;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Courses.Commands.CreateCourse;

public class CreateCourseCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateCourseCommandHandler _sut;

    public CreateCourseCommandHandlerTests()
    {
        _sut = new CreateCourseCommandHandler(_currentUserService, _categoryRepository, _courseRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var command = new CreateCourseCommand(Guid.NewGuid(), "Title", "Desc", 0m, new List<string>());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotInstructorOrAdmin()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Instructor).Returns(false);
        _currentUserService.IsInRole(Roles.Admin).Returns(false);
        var command = new CreateCourseCommand(Guid.NewGuid(), "Title", "Desc", 0m, new List<string>());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ForbiddenError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CourseMessages.OnlyInstructorsCreateCourses);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCategoryNotFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Instructor).Returns(true);

        _categoryRepository.AnyAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateCourseCommand(categoryId, "Title", "Desc", 0m, new List<string>());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.CourseCategoryNotFound(categoryId));
    }

    [Fact]
    public async Task Handle_ShouldCreateCourseAndNormalizeTags_WhenSuccessful()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _currentUserService.UserId.Returns(instructorId);
        _currentUserService.IsInRole(Roles.Instructor).Returns(true);

        _categoryRepository.AnyAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var tags = new List<string> { " C# ", " ASP.NET ", "c#", "", " " };
        var command = new CreateCourseCommand(categoryId, " Title ", "Desc", 10.5m, tags);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CourseId.Should().NotBeEmpty();

        await _courseRepository.Received(1).AddAsync(Arg.Is<Course>(c =>
            c.InstructorId == instructorId &&
            c.CategoryId == categoryId &&
            c.Title == "Title" &&
            c.Price == 10.5m &&
            c.Tags.Count == 2 && c.Tags.Contains("C#") && c.Tags.Contains("ASP.NET")
        ), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
