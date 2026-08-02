using FluentResults;
using Learnix.Application.Categories.Commands.UpdateCategory;
using Learnix.Application.Categories.Constants;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute.ReturnsExtensions;

namespace Learnix.Application.UnitTests.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly UpdateCategoryCommandHandler _sut;

    public UpdateCategoryCommandHandlerTests()
    {
        _sut = new UpdateCategoryCommandHandler(
            _categoryRepository,
            _blobStorage,
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

        var command = new UpdateCategoryCommand(categoryId, "Test", "test", null, false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.CourseCategoryNotFound(categoryId));
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenSlugIsInUseByAnotherCategory()
    {
        // Arrange

        var category = Category.Create("Old Name", "old-slug");
        var existingCategory = Category.Create("Existing", "new-slug");

        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>())
            .Returns(existingCategory);

        var command = new UpdateCategoryCommand(category.Id, "New Name", "new-slug", null, false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ConflictError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CategoryMessages.CategorySlugInUse("new-slug"));
    }

    [Fact]
    public async Task Handle_ShouldUpdateCategoryAndClearCache_WhenSuccessful()
    {
        // Arrange

        var category = Category.Create("Old Name", "old-slug");

        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var command = new UpdateCategoryCommand(category.Id, " New Name ", " new-slug ", null, false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("New Name");
        category.Slug.Should().Be("new-slug");

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.Categories.All, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSetImage_WhenImageBlobPathProvided()
    {
        // Arrange

        var category = Category.Create("Test", "test");
        var uploadResult = new BlobMetadata("category-images/final.jpg", "image/jpeg", 1024);

        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();
        _blobStorage.CommitUploadAsync("temp/image.jpg", UploadTarget.CategoryImage, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(uploadResult));

        var command = new UpdateCategoryCommand(category.Id, "Test", "test", "temp/image.jpg", false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        category.ImageBlobPath.Should().Be("category-images/final.jpg");

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.Categories.All, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCommitUploadFails()
    {
        // Arrange

        var category = Category.Create("Test", "test");

        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();
        _blobStorage.CommitUploadAsync("temp/image.jpg", UploadTarget.CategoryImage, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<BlobMetadata>("Upload error"));

        var command = new UpdateCategoryCommand(category.Id, "Test", "test", "temp/image.jpg", false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Upload error");
    }

    [Fact]
    public async Task Handle_ShouldClearImage_WhenRemoveImageIsTrue()
    {
        // Arrange

        var category = Category.Create("Test", "test");
        category.SetImage("category-images/existing.jpg");

        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var command = new UpdateCategoryCommand(category.Id, "Test", "test", null, true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        category.ImageBlobPath.Should().BeNull();

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.Categories.All, Arg.Any<CancellationToken>());
    }
}
