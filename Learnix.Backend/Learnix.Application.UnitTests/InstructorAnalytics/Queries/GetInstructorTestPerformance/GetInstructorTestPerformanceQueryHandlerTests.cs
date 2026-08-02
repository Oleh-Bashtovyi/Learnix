using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
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
    private static readonly Guid ForeignCourseId = Guid.NewGuid();
    private readonly Course _ownedCourse;
    private readonly TestLesson _test;

    public GetInstructorTestPerformanceQueryHandlerTests()
    {
        _currentUser.UserId.Returns(InstructorId);
        _sut = new GetInstructorTestPerformanceQueryHandler(_currentUser, _courseRepository, _testAttemptRepository);

        _ownedCourse = Course.Create(InstructorId, Guid.NewGuid(), "React", "…", 0m);
        var section = _ownedCourse.AddSection("Basics");
        _test = TestLesson.Create(section.Id, "Quiz");
        _ownedCourse.AddLesson(_test);

        _courseRepository
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns([_ownedCourse]);
    }

    private Task<FluentResults.Result<List<InstructorTestPerformanceDto>>> Act(Guid? courseId = null) =>
        _sut.Handle(new GetInstructorTestPerformanceQuery(courseId), CancellationToken.None);

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
    public async Task Filtering_by_a_course_the_instructor_does_not_own_is_forbidden_not_an_empty_result()
    {
        var result = await Act(ForeignCourseId);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _testAttemptRepository.DidNotReceiveWithAnyArgs()
            .GetPerformanceByTestAsync(default!, default);
    }

    [Fact]
    public async Task Filtering_by_an_owned_course_narrows_the_query_to_it()
    {
        _testAttemptRepository
            .GetPerformanceByTestAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new TestPerformanceBucket(_ownedCourse.Id, _test.Id, TotalAttempts: 4, AverageScore: 7.5, MaxScore: 10, PassedCount: 3)]);

        var result = await Act(_ownedCourse.Id);

        result.IsSuccess.Should().BeTrue();
        await _testAttemptRepository.Received(1).GetPerformanceByTestAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == _ownedCourse.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scoring_is_aggregated_in_the_database_and_joined_with_the_lesson_title_in_memory()
    {
        _testAttemptRepository
            .GetPerformanceByTestAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new TestPerformanceBucket(_ownedCourse.Id, _test.Id, TotalAttempts: 4, AverageScore: 7.5, MaxScore: 10, PassedCount: 3)]);

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Should().ContainSingle().Subject;
        dto.CourseTitle.Should().Be("React");
        dto.LessonTitle.Should().Be("Quiz");
        dto.AverageScore.Should().Be(7.5);
        dto.MaxScore.Should().Be(10);
        dto.PassRate.Should().Be(0.75);
        dto.AttemptsCount.Should().Be(4);
        await _testAttemptRepository.Received(1).GetPerformanceByTestAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == _ownedCourse.Id),
            Arg.Any<CancellationToken>());
    }
}
