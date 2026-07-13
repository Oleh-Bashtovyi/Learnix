using Learnix.Application.Categories.Queries.GetAdminCategories;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Categories.Queries.GetAdminCategories;

public class GetAdminCategoriesQueryHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetAdminCategoriesQueryHandler _sut;

    public GetAdminCategoriesQueryHandlerTests()
    {
        _sut = new GetAdminCategoriesQueryHandler(_categoryRepository, _blobStorage, _currentUserService);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var query = new GetAdminCategoriesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

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
        var query = new GetAdminCategoriesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<ForbiddenError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.OnlyAdminCanManageCategories);
    }

    [Fact]
    public async Task Handle_ShouldReturnCategories_WhenUserIsAdmin()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        var category1 = Category.Create("Test 1", "test-1");
        var category2 = Category.CreateSystem("System Test", "system-test");
        category1.SetImage("path/to/image1.jpg");

        _categoryRepository.ListAsync(Arg.Any<CategoriesOrderedSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category> { category1, category2 });

        _blobStorage.GetPublicUrl("path/to/image1.jpg").Returns("http://example.com/image1.jpg");

        var query = new GetAdminCategoriesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var first = result.Value.First(c => c.Id == category1.Id);
        first.Name.Should().Be("Test 1");
        first.ImageUrl.Should().Be("http://example.com/image1.jpg");
        first.IsSystem.Should().BeFalse();

        var second = result.Value.First(c => c.Id == category2.Id);
        second.Name.Should().Be("System Test");
        second.ImageUrl.Should().BeNull();
        second.IsSystem.Should().BeTrue();
    }
}
