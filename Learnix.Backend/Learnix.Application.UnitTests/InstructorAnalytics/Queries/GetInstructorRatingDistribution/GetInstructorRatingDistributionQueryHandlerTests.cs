using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingDistribution;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.InstructorAnalytics.Queries.GetInstructorRatingDistribution;

public class GetInstructorRatingDistributionQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly ICourseReviewRepository _reviewRepository = Substitute.For<ICourseReviewRepository>();
    private readonly GetInstructorRatingDistributionQueryHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();
    private static readonly Guid ForeignCourseId = Guid.NewGuid();
    private readonly Guid _ownedCourseId;

    public GetInstructorRatingDistributionQueryHandlerTests()
    {
        _currentUser.UserId.Returns(InstructorId);
        _sut = new GetInstructorRatingDistributionQueryHandler(_currentUser, _courseRepository, _reviewRepository);

        var owned = Course.Create(InstructorId, Guid.NewGuid(), "React", "…", 0m);
        _ownedCourseId = owned.Id;
        OwnedCourses(owned);
    }

    private void OwnedCourses(params Course[] courses) =>
        _courseRepository
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(courses.ToList());

    private Task<FluentResults.Result<InstructorRatingDistributionDto>> Act(Guid? courseId = null) =>
        _sut.Handle(new GetInstructorRatingDistributionQuery(courseId), CancellationToken.None);

    [Fact]
    public async Task Filtering_by_a_course_the_instructor_does_not_own_is_forbidden_not_an_empty_result()
    {
        var result = await Act(ForeignCourseId);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _reviewRepository.DidNotReceiveWithAnyArgs()
            .GetRatingDistributionAsync(default!, default);
    }

    [Fact]
    public async Task Filtering_by_an_owned_course_narrows_the_query_to_it()
    {
        _reviewRepository
            .GetRatingDistributionAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int> { [5] = 3 });

        var result = await Act(_ownedCourseId);

        result.IsSuccess.Should().BeTrue();
        result.Value.FiveStar.Should().Be(3);
        await _reviewRepository.Received(1).GetRatingDistributionAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == _ownedCourseId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_filter_aggregates_across_every_owned_course()
    {
        _reviewRepository
            .GetRatingDistributionAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int> { [4] = 2 });

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        result.Value.FourStar.Should().Be(2);
        await _reviewRepository.Received(1).GetRatingDistributionAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == _ownedCourseId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_instructor_with_no_courses_and_no_filter_gets_an_all_zero_result_not_an_error()
    {
        OwnedCourses();

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new InstructorRatingDistributionDto(0, 0, 0, 0, 0));
    }
}
