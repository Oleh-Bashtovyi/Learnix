using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.Reviews.Commands.DeleteReview;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.UnitTests.Reviews.Commands.DeleteReview;

public class DeleteReviewCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly ICourseReviewRepository _reviewRepository = Substitute.For<ICourseReviewRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly DeleteReviewCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid ReviewId = Guid.NewGuid();

    public DeleteReviewCommandHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);

        // The handler's real work happens inside the transaction callback; run it as the database would.
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<Task>>().Invoke());

        _reviewRepository
            .GetCourseRatingMetricsAsync(CourseId, Arg.Any<CancellationToken>())
            .Returns((2, 4.5m));

        _sut = new DeleteReviewCommandHandler(
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

    private Task<FluentResults.Result> Act() =>
        _sut.Handle(new DeleteReviewCommand(CourseId, ReviewId), CancellationToken.None);

    [Fact]
    public async Task Deleting_your_own_review_resyncs_the_course_rating()
    {
        // Arrange
        var course = CourseWithRating();
        ReviewIs(CourseReview.Create(CourseId, StudentId, 5, "Great"));
        CourseIs(course);

        // Act
        var result = await Act();

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _reviewRepository.Received(1).DeleteAsync(Arg.Any<CourseReview>(), Arg.Any<CancellationToken>());
        course.ReviewsCount.Should().Be(2);
        course.AverageRating.Should().Be(4.5m);
        await _cache.Received(1).RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_admin_may_delete_somebody_elses_review()
    {
        // Arrange
        _currentUser.IsInRole(Roles.Admin).Returns(true);
        ReviewIs(CourseReview.Create(CourseId, Guid.NewGuid(), 1, "Spam"));
        CourseIs(CourseWithRating());

        // Act
        var result = await Act();

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _reviewRepository.Received(1).DeleteAsync(Arg.Any<CourseReview>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_student_may_not_delete_somebody_elses_review()
    {
        // Arrange
        ReviewIs(CourseReview.Create(CourseId, Guid.NewGuid(), 5, "Not yours"));

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _reviewRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    /// <summary>
    /// A review whose id exists but belongs to another course is not this course's review — otherwise the
    /// course id in the route would be decoration, and the rating of the wrong course would be resynced.
    /// </summary>
    [Fact]
    public async Task A_review_from_another_course_is_not_found()
    {
        // Arrange
        ReviewIs(CourseReview.Create(Guid.NewGuid(), StudentId, 5, "Elsewhere"));

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    /// <summary>
    /// The course can be soft-deleted while its reviews live on. The delete used to be skipped whole and
    /// still answer Ok — the caller was told the review was gone while it sat there untouched.
    /// </summary>
    [Fact]
    public async Task A_soft_deleted_course_does_not_turn_the_delete_into_a_silent_no_op()
    {
        // Arrange
        ReviewIs(CourseReview.Create(CourseId, StudentId, 5, "Great"));
        CourseIs(null);

        // Act
        var result = await Act();

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _reviewRepository.Received(1).DeleteAsync(Arg.Any<CourseReview>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Course CourseWithRating() =>
        Course.Create(Guid.NewGuid(), Guid.NewGuid(), "React", "…", 0m);
}
