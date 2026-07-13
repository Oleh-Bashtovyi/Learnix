using Learnix.Application.Categories.Queries.GetAllCategories;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Categories.Queries.GetAllCategories;

public class GetAllCategoriesQueryHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly GetAllCategoriesQueryHandler _sut;

    public GetAllCategoriesQueryHandlerTests()
    {
        _sut = new GetAllCategoriesQueryHandler(_categoryRepository, _blobStorage);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoCategoriesExist()
    {
        // Arrange
        _categoryRepository.ListAsync(Arg.Any<CategoriesOrderedSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>());

        var query = new GetAllCategoriesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnCategories_WithImageUrls_WhenImagesExist()
    {
        // Arrange
        var category1 = Category.Create("Test 1", "test-1");
        var category2 = Category.Create("Test 2", "test-2");
        category1.SetImage("path/to/image1.jpg");
        // category2 has no image

        _categoryRepository.ListAsync(Arg.Any<CategoriesOrderedSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category> { category1, category2 });

        _blobStorage.GetPublicUrl("path/to/image1.jpg").Returns("http://example.com/image1.jpg");

        var query = new GetAllCategoriesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var first = result.Value.First(c => c.Id == category1.Id);
        first.Name.Should().Be("Test 1");
        first.ImageUrl.Should().Be("http://example.com/image1.jpg");

        var second = result.Value.First(c => c.Id == category2.Id);
        second.Name.Should().Be("Test 2");
        second.ImageUrl.Should().BeNull();
    }
}
