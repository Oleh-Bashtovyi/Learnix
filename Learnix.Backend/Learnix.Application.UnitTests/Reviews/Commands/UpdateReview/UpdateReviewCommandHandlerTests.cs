using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.Reviews.Commands.UpdateReview;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.UnitTests.Reviews.Commands.UpdateReview;

public class UpdateReviewCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly ICourseReviewRepository _reviewRepository = Substitute.For<ICourseReviewRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly UpdateReviewCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid ReviewId = Guid.NewGuid();

    public UpdateReviewCommandHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);

        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<Task>>().Invoke());

        _reviewRepository
            .GetCourseRatingMetricsAsync(CourseId, Arg.Any<CancellationToken>())
            .Returns((3, 4.0m));

        _sut = new UpdateReviewCommandHandler(
            _currentUser, _courseRepository, _reviewRepository, _unitOfWork, _cache);
    }

    private void ReviewIs(CourseReview? review) =>
        _reviewRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<CourseReview>>(), Arg.Any<CancellationToken>())
            .Returns(review);

    private void CourseIs(Course? course) =>
        _courseRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(course);

    private Task<FluentResults.Result> Act(int rating = 4, string? comment = "Updated") =>
        _sut.Handle(new UpdateReviewCommand(CourseId, ReviewId, rating, comment), CancellationToken.None);

    [Fact]
    public async Task Editing_your_own_review_rewrites_it_and_resyncs_the_course_rating()
    {
        var review = CourseReview.Create(CourseId, StudentId, 2, "Meh");
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "React", "…", 0m);
        ReviewIs(review);
        CourseIs(course);

        var result = await Act(rating: 4, comment: "Better than I thought");

        result.IsSuccess.Should().BeTrue();
        review.Rating.Should().Be(4);
        review.Comment.Should().Be("Better than I thought");
        course.ReviewsCount.Should().Be(3);
        course.AverageRating.Should().Be(4.0m);
    }

    /// <summary>An admin is trusted to delete a review, never to rewrite one in somebody's name.</summary>
    [Fact]
    public async Task Not_even_an_admin_may_edit_somebody_elses_review()
    {
        _currentUser.IsInRole(Roles.Admin).Returns(true);
        ReviewIs(CourseReview.Create(CourseId, Guid.NewGuid(), 5, "Not yours"));

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task A_review_from_another_course_is_not_found()
    {
        ReviewIs(CourseReview.Create(Guid.NewGuid(), StudentId, 5, "Elsewhere"));

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    /// <summary>The edit used to be skipped entirely when the course was gone — and still answered Ok.</summary>
    [Fact]
    public async Task A_soft_deleted_course_does_not_turn_the_edit_into_a_silent_no_op()
    {
        var review = CourseReview.Create(CourseId, StudentId, 2, "Meh");
        ReviewIs(review);
        CourseIs(null);

        var result = await Act(rating: 5, comment: "Rewritten");

        result.IsSuccess.Should().BeTrue();
        review.Rating.Should().Be(5);
        review.Comment.Should().Be("Rewritten");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
