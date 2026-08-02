using Learnix.Application.Categories.Commands.DeleteCategory;
using Learnix.Application.Categories.Constants;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute.ReturnsExtensions;

namespace Learnix.Application.UnitTests.Categories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly DeleteCategoryCommandHandler _sut;

    public DeleteCategoryCommandHandlerTests()
    {
        _sut = new DeleteCategoryCommandHandler(
            _categoryRepository,
            _courseRepository,
            _unitOfWork,
            _cache);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCategoryNotFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var command = new DeleteCategoryCommand(categoryId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.CourseCategoryNotFound(categoryId));
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCategoryIsSystem()
    {

        var category = Category.CreateSystem("Test", "test");

        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(category);

        var command = new DeleteCategoryCommand(category.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ConflictError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CategoryMessages.SystemCategoriesCannotBeDeleted);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCategoryIsUsedByCourses()
    {

        var category = Category.Create("Test", "test");

        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(category);

        _courseRepository.AnyAsync(Arg.Any<CourseByCategoryIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new DeleteCategoryCommand(category.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ConflictError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CategoryMessages.CategoryUsedByCourses);
    }

    [Fact]
    public async Task Handle_ShouldDeleteCategoryAndClearCache_WhenSuccessful()
    {

        var category = Category.Create("Test", "test");

        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(category);

        _courseRepository.AnyAsync(Arg.Any<CourseByCategoryIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new DeleteCategoryCommand(category.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _categoryRepository.Received(1).DeleteAsync(category, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.Categories.All, Arg.Any<CancellationToken>());
    }
}
