using Ardalis.Specification;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Queries.GetPublishedCourseCount;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Courses.Queries.GetPublishedCourseCount;

public class GetPublishedCourseCountQueryHandlerTests
{
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly GetPublishedCourseCountQueryHandler _sut;

    public GetPublishedCourseCountQueryHandlerTests()
    {
        _sut = new GetPublishedCourseCountQueryHandler(_courseRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnCountFromPublishedStatusSpecification()
    {
        // Arrange
        _courseRepository
            .CountAsync(Arg.Any<AdminCoursesByStatusCountSpecification>(), Arg.Any<CancellationToken>())
            .Returns(42);

        // Act
        var result = await _sut.Handle(new GetPublishedCourseCountQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);

        await _courseRepository.Received(1).CountAsync(
            Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>());
    }
}
