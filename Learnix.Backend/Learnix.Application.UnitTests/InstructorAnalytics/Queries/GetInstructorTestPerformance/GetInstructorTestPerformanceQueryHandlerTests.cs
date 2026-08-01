using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorTestPerformance;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.InstructorAnalytics.Queries.GetInstructorTestPerformance;

public class GetInstructorTestPerformanceQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly ITestAttemptRepository _testAttemptRepository = Substitute.For<ITestAttemptRepository>();
    private readonly GetInstructorTestPerformanceQueryHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();

    public GetInstructorTestPerformanceQueryHandlerTests()
    {
        _currentUser.UserId.Returns(InstructorId);
        _sut = new GetInstructorTestPerformanceQueryHandler(_currentUser, _courseRepository, _testAttemptRepository);
    }

    private Task<FluentResults.Result<List<InstructorTestPerformanceDto>>> Act() =>
        _sut.Handle(new GetInstructorTestPerformanceQuery(), CancellationToken.None);

    [Fact]
    public async Task An_instructor_with_no_courses_never_queries_attempts()
    {
        _courseRepository
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _testAttemptRepository.DidNotReceiveWithAnyArgs()
            .GetPerformanceByTestAsync(default!, default);
    }

    [Fact]
    public async Task Scoring_is_aggregated_in_the_database_and_joined_with_the_lesson_title_in_memory()
    {
        var course = Course.Create(InstructorId, Guid.NewGuid(), "React", "…", 0m);
        var section = course.AddSection("Basics");
        var test = TestLesson.Create(section.Id, "Quiz");
        course.AddLesson(test);

        _courseRepository
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns([course]);
        _testAttemptRepository
            .GetPerformanceByTestAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new TestPerformanceBucket(course.Id, test.Id, TotalAttempts: 4, AverageScore: 7.5, MaxScore: 10, PassedCount: 3)]);

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Should().ContainSingle().Subject;
        dto.CourseTitle.Should().Be("React");
        dto.LessonTitle.Should().Be("Quiz");
        dto.AverageScore.Should().Be(7.5);
        dto.MaxScore.Should().Be(10);
        dto.PassRate.Should().Be(0.75);
        await _testAttemptRepository.Received(1).GetPerformanceByTestAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == course.Id),
            Arg.Any<CancellationToken>());
    }
}
