using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Queries.GetFeaturedCourses;

namespace Learnix.Application.UnitTests.Courses.Queries.GetFeaturedCourses;

public class GetFeaturedCoursesQueryHandlerTests
{
    private readonly IFeaturedCoursesService _featuredCoursesService = Substitute.For<IFeaturedCoursesService>();
    private readonly GetFeaturedCoursesQueryHandler _sut;

    public GetFeaturedCoursesQueryHandlerTests()
    {
        _sut = new GetFeaturedCoursesQueryHandler(_featuredCoursesService);
    }

    [Fact]
    public async Task Handle_ShouldReturnFeaturedCourses()
    {
        // Arrange
        var courses = new List<FeaturedCourseDto>
        {
            new FeaturedCourseDto(Guid.NewGuid(), "Title", "Desc", null, 10m, false, 4.5m, 10, 5.0, "Category", new FeaturedCourseInstructorDto(Guid.NewGuid(), "John Doe"), null)
        };

        _featuredCoursesService.GetTopFeaturedAsync(6, Arg.Any<CancellationToken>())
            .Returns(courses);

        var query = new GetFeaturedCoursesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(courses);
    }
}
