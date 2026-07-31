using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Queries.SearchCourses;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.AiChat.Queries.SearchCourses;

public class SearchCoursesQueryHandlerTests
{
    private readonly IAiCourseSearchService _searchService = Substitute.For<IAiCourseSearchService>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly SearchCoursesQueryHandler _sut;

    public SearchCoursesQueryHandlerTests()
    {
        _sut = new SearchCoursesQueryHandler(_searchService, _categoryRepository);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToTheSharedSearchService_WithoutResolvingCategory_WhenNoCategoryGiven()
    {
        // Arrange
        _searchService
            .SearchAsync("python", null, 10, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CourseSearchResultDto>)[]);

        // Act
        var result = await _sut.Handle(new SearchCoursesQuery("python"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _categoryRepository.DidNotReceive()
            .FirstOrDefaultAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldResolveCategorySlugToId_BeforeSearching()
    {
        // Arrange
        var category = Category.Create("Programming", "programming");
        _categoryRepository
            .FirstOrDefaultAsync(Arg.Any<CategoryBySlugSpecification>(), Arg.Any<CancellationToken>())
            .Returns(category);

        _searchService
            .SearchAsync("python", category.Id, 10, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CourseSearchResultDto>)[]);

        // Act
        var result = await _sut.Handle(
            new SearchCoursesQuery("python", "programming"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _searchService.Received(1).SearchAsync(
            "python", category.Id, 10, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 20)]
    public async Task Handle_ShouldClampMaxResultsBetween1And20(int requested, int expectedClamped)
    {
        // Arrange
        _searchService
            .SearchAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CourseSearchResultDto>)[]);

        // Act
        await _sut.Handle(new SearchCoursesQuery("python", MaxResults: requested), CancellationToken.None);

        // Assert
        await _searchService.Received(1).SearchAsync(
            "python", null, expectedClamped, Arg.Any<CancellationToken>());
    }
}
