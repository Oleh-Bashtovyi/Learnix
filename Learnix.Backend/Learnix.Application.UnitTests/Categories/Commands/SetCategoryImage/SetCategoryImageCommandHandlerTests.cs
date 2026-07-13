using FluentResults;
using Learnix.Application.Categories.Commands.SetCategoryImage;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute.ReturnsExtensions;

namespace Learnix.Application.UnitTests.Categories.Commands.SetCategoryImage;

public class SetCategoryImageCommandHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly SetCategoryImageCommandHandler _sut;

    public SetCategoryImageCommandHandlerTests()
    {
        _sut = new SetCategoryImageCommandHandler(
            _categoryRepository,
            _blobStorage,
            _unitOfWork,
            _currentUserService,
            _cache);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var command = new SetCategoryImageCommand(Guid.NewGuid(), "temp/path.jpg");

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
        var command = new SetCategoryImageCommand(Guid.NewGuid(), "temp/path.jpg");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ForbiddenError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.OnlyAdminCanManageCategories);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCommitUploadFails()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        _blobStorage.CommitUploadAsync("temp/path.jpg", UploadTarget.CategoryImage, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<BlobMetadata>("Upload error"));

        var command = new SetCategoryImageCommand(Guid.NewGuid(), "temp/path.jpg");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Upload error");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCategoryNotFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        var uploadResult = new BlobMetadata("final/path.jpg", "image/jpeg", 1024);
        _blobStorage.CommitUploadAsync("temp/path.jpg", UploadTarget.CategoryImage, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(uploadResult));

        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var command = new SetCategoryImageCommand(categoryId, "temp/path.jpg");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.CourseCategoryNotFound(categoryId));
    }

    [Fact]
    public async Task Handle_ShouldSetImageAndClearCache_WhenSuccessful()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        var category = Category.Create("Test", "test");
        var uploadResult = new BlobMetadata("final/path.jpg", "image/jpeg", 1024);

        _blobStorage.CommitUploadAsync("temp/path.jpg", UploadTarget.CategoryImage, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(uploadResult));

        _categoryRepository.FirstOrDefaultAsync(Arg.Any<CategoryByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(category);

        var command = new SetCategoryImageCommand(category.Id, "temp/path.jpg");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        category.ImageBlobPath.Should().Be("final/path.jpg");

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.Categories.All, Arg.Any<CancellationToken>());
    }
}
