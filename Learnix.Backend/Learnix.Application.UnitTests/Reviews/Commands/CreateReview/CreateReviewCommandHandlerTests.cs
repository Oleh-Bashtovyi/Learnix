using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.Reviews.Commands.CreateReview;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.UnitTests.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ICourseReviewRepository _reviewRepository = Substitute.For<ICourseReviewRepository>();
    private readonly ILessonProgressRepository _lessonProgressRepository = Substitute.For<ILessonProgressRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();

    private readonly CreateReviewCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid InstructorId = Guid.NewGuid();

    private readonly Course _course = Course.Create(InstructorId, Guid.NewGuid(), "React", "Learn React", 49m);

    public CreateReviewCommandHandlerTests()
    {
        _sut = new CreateReviewCommandHandler(
            _currentUser, _courseRepository, _enrollmentRepository, _reviewRepository,
            _lessonProgressRepository, _unitOfWork, _cache);

        // Default: an enrolled student who has started the course and has not reviewed it yet
        _currentUser.UserId.Returns(StudentId);
        StubCourse(_course);
        StubEnrolled(true);
        StubAlreadyReviewed(false);
        StubProgress(completed: 3, total: 10);
        StubRatingMetrics(count: 1, average: 5m);
        RunTransactionBody();
    }

    // Guards

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnAuthenticationError()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
    }

    [Fact]
    public async Task Handle_WhenCourseDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        StubCourse(null);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Handle_WhenTheInstructorReviewsTheirOwnCourse_ShouldReturnForbidden()
    {
        // Arrange
        _currentUser.UserId.Returns(InstructorId);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenError>();
        await _enrollmentRepository.DidNotReceive()
            .AnyAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStudentIsNotEnrolled_ShouldReturnForbidden()
    {
        // Arrange
        StubEnrolled(false);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenError>();
    }

    [Fact]
    public async Task Handle_WhenStudentAlreadyReviewedTheCourse_ShouldReturnConflictAndWriteNothing()
    {
        // Arrange
        StubAlreadyReviewed(true);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictError>();

        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStudentHasNotCompletedAnyLesson_ShouldReturnForbiddenAndWriteNothing()
    {
        // Arrange — enrolled but has not finished a single lesson yet
        StubProgress(completed: 0, total: 10);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenError>();

        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // Happy path

    [Fact]
    public async Task Handle_WhenReviewIsValid_ShouldPersistItAndReturnItsId()
    {
        // Arrange
        CourseReview? captured = null;
        await _reviewRepository.AddAsync(
            Arg.Do<CourseReview>(r => captured = r), Arg.Any<CancellationToken>());
        _reviewRepository.ClearReceivedCalls();

        // Act
        var result = await _sut.Handle(Command(rating: 4, comment: "Solid course"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.CourseId.Should().Be(_course.Id);
        captured.StudentId.Should().Be(StudentId);
        captured.Rating.Should().Be(4);
        captured.Comment.Should().Be("Solid course");
        result.Value.ReviewId.Should().Be(captured.Id);
    }

    [Fact]
    public async Task Handle_WhenReviewIsValid_ShouldSnapshotTheStudentsProgress()
    {
        // Arrange
        StubProgress(completed: 6, total: 8);
        CourseReview? captured = null;
        await _reviewRepository.AddAsync(
            Arg.Do<CourseReview>(r => captured = r), Arg.Any<CancellationToken>());

        // Act
        await _sut.Handle(Command(), default);

        // Assert — the counts at review time are recorded for later credibility weighting
        captured.Should().NotBeNull();
        captured!.CompletedLessonsAtReview.Should().Be(6);
        captured.TotalLessonsAtReview.Should().Be(8);
    }

    [Fact]
    public async Task Handle_WhenReviewIsAdded_ShouldSyncTheDenormalisedRatingFromTheRecomputedMetrics()
    {
        // Arrange — the handler must take the aggregate from the repository, not from the submitted rating
        StubRatingMetrics(count: 7, average: 4.25m);

        // Act
        await _sut.Handle(Command(rating: 1), default);

        // Assert
        _course.ReviewsCount.Should().Be(7);
        _course.AverageRating.Should().Be(4.25m);
    }

    [Fact]
    public async Task Handle_WhenReviewIsAdded_ShouldWriteInsideATransaction()
    {
        // Arrange — the insert and the rating sync must not be able to land separately
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());

        // Act
        await _sut.Handle(Command(), default);

        // Assert
        await _unitOfWork.Received(1)
            .ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
        await _reviewRepository.Received(1).AddAsync(Arg.Any<CourseReview>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReviewIsAdded_ShouldEvictTheCachedCourse()
    {
        // Act
        await _sut.Handle(Command(), default);

        // Assert — a stale course:{id} entry would still carry the old rating
        await _cache.Received(1).RemoveAsync(CacheKeys.Courses.ById(_course.Id), Arg.Any<CancellationToken>());
    }

    // Fixtures

    private CreateReviewCommand Command(int rating = 5, string? comment = null) =>
        new(_course.Id, rating, comment);

    private void StubCourse(Course? course) =>
        _courseRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(course);

    private void StubEnrolled(bool enrolled) =>
        _enrollmentRepository
            .AnyAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(enrolled);

    private void StubAlreadyReviewed(bool reviewed) =>
        _reviewRepository
            .AnyAsync(Arg.Any<ISpecification<CourseReview>>(), Arg.Any<CancellationToken>())
            .Returns(reviewed);

    private void StubProgress(int completed, int total) =>
        _lessonProgressRepository
            .GetProgressCountsAsync(StudentId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, CourseProgressCounts> { [_course.Id] = new(completed, total) });

    private void StubRatingMetrics(int count, decimal average) =>
        _reviewRepository
            .GetCourseRatingMetricsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((count, average));

    /// <summary>
    /// The substituted <see cref="IUnitOfWork"/> would otherwise never invoke the transaction body,
    /// leaving the insert and the rating sync unexecuted.
    /// </summary>
    private void RunTransactionBody() =>
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>().Invoke());
}
