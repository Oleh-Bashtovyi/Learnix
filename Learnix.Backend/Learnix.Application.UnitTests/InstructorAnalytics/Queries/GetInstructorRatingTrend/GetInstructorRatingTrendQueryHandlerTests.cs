using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingTrend;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.InstructorAnalytics.Queries.GetInstructorRatingTrend;

public class GetInstructorRatingTrendQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly ICourseReviewRepository _reviewRepository = Substitute.For<ICourseReviewRepository>();
    private readonly GetInstructorRatingTrendQueryHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();
    private static readonly Guid ForeignCourseId = Guid.NewGuid();
    private readonly Guid _ownedCourseId;

    public GetInstructorRatingTrendQueryHandlerTests()
    {
        _currentUser.UserId.Returns(InstructorId);
        _sut = new GetInstructorRatingTrendQueryHandler(_currentUser, _courseRepository, _reviewRepository);

        var owned = Course.Create(InstructorId, Guid.NewGuid(), "React", "…", 0m);
        _ownedCourseId = owned.Id;
        _courseRepository
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns([owned]);

        _reviewRepository
            .GetMonthlyRatingTrendAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private Task<FluentResults.Result<List<InstructorRatingTrendItemDto>>> Act(Guid? courseId = null) =>
        _sut.Handle(new GetInstructorRatingTrendQuery(courseId), CancellationToken.None);

    [Fact]
    public async Task Filtering_by_a_course_the_instructor_does_not_own_is_forbidden()
    {
        var result = await Act(ForeignCourseId);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _reviewRepository.DidNotReceiveWithAnyArgs()
            .GetMonthlyRatingTrendAsync(default!, default);
    }

    [Fact]
    public async Task Filtering_by_an_owned_course_narrows_the_query_to_it()
    {
        var result = await Act(_ownedCourseId);

        result.IsSuccess.Should().BeTrue();
        await _reviewRepository.Received(1).GetMonthlyRatingTrendAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == _ownedCourseId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_filter_aggregates_across_every_owned_course()
    {
        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        await _reviewRepository.Received(1).GetMonthlyRatingTrendAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == _ownedCourseId),
            Arg.Any<CancellationToken>());
    }
}
