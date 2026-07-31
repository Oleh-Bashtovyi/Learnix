using Learnix.Application.AiChat.Queries.GetCategories;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.AiChat.Queries.GetCategories;

public class GetCategoriesQueryHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly GetCategoriesQueryHandler _sut;

    public GetCategoriesQueryHandlerTests()
    {
        _sut = new GetCategoriesQueryHandler(_categoryRepository);
    }

    [Fact]
    public async Task Handle_ShouldOrderByCoursesCountDescending_UnlikeTheCatalogsAlphabeticalOrder()
    {
        // Arrange
        var popular = Category.Create("Programming", "programming");
        popular.IncrementCoursesCount();
        popular.IncrementCoursesCount();

        var niche = Category.Create("Anthropology", "anthropology");
        niche.IncrementCoursesCount();

        var empty = Category.Create("Zoology", "zoology");

        // The repository call returns the shared, alphabetically-ordered spec's result — the handler
        // re-sorts it, it does not rely on the database to hand back the order it needs.
        _categoryRepository
            .ListAsync(Arg.Any<CategoriesOrderedSpecification>(), Arg.Any<CancellationToken>())
            .Returns([empty, niche, popular]);

        // Act
        var result = await _sut.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(c => c.Slug).Should().ContainInOrder("programming", "anthropology", "zoology");
    }
}
