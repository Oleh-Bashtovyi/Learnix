using FluentResults;
using Learnix.Application.Categories.Commands.CreateCategory;
using Learnix.Application.Categories.Constants;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.UnitTests.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly CreateCategoryCommandHandler _sut;

    public CreateCategoryCommandHandlerTests()
    {
        _sut = new CreateCategoryCommandHandler(
            _categoryRepository,
            _blobStorage,
            _unitOfWork,
            _cache);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenSlugIsAlreadyInUse()
    {
        // Arrange
        _categoryRepository.AnyAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateCategoryCommand("Test", "test", null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ConflictError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CategoryMessages.CategorySlugInUse("test"));
    }

    [Fact]
    public async Task Handle_ShouldCreateCategoryAndClearCache_WhenSuccessful()
    {
        // Arrange
        _categoryRepository.AnyAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateCategoryCommand(" Test Category ", " test-category ", null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _categoryRepository.Received(1).AddAsync(
            Arg.Is<Category>(c => c.Name == "Test Category" && c.Slug == "test-category"),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        await _cache.Received(1).RemoveAsync(CacheKeys.Categories.All, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSetImage_WhenImageBlobPathProvided()
    {
        // Arrange
        _categoryRepository.AnyAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var uploadResult = new BlobMetadata("category-images/final.jpg", "image/jpeg", 1024);
        _blobStorage.CommitUploadAsync("temp/image.jpg", UploadTarget.CategoryImage, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(uploadResult));

        var command = new CreateCategoryCommand("Test", "test", "temp/image.jpg");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _categoryRepository.Received(1).AddAsync(
            Arg.Is<Category>(c => c.ImageBlobPath == "category-images/final.jpg"),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCommitUploadFails()
    {
        // Arrange
        _categoryRepository.AnyAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _blobStorage.CommitUploadAsync("temp/image.jpg", UploadTarget.CategoryImage, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<BlobMetadata>("Upload error"));

        var command = new CreateCategoryCommand("Test", "test", "temp/image.jpg");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Upload error");
    }
}
