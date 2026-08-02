using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.TestAttempts.Commands.StartTestAttempt;
using Learnix.Domain.Entities;
using Learnix.Domain.ValueObjects;

namespace Learnix.Application.UnitTests.TestAttempts.Commands.StartTestAttempt;

public class StartTestAttemptCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ILessonRepository _lessonRepository = Substitute.For<ILessonRepository>();
    private readonly ITestAttemptRepository _testAttemptRepository = Substitute.For<ITestAttemptRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly StartTestAttemptCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid LessonId = Guid.NewGuid();
    private static readonly Guid VersionId = Guid.NewGuid();

    private static readonly StartTestAttemptCommand Command = new(CourseId, LessonId);

    public StartTestAttemptCommandHandlerTests()
    {
        _sut = new StartTestAttemptCommandHandler(
            _currentUser, _enrollmentRepository, _lessonRepository, _testAttemptRepository, _unitOfWork);

        // Default: an enrolled student facing a test with no limit and no cooldown
        _currentUser.UserId.Returns(StudentId);
        StubEnrolled(true);
        StubTestLesson(Test());
        StubSubmittedAttempts();
    }

    // Guards

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnAuthenticationError()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
    }

    [Fact]
    public async Task Handle_WhenStudentIsNotEnrolled_ShouldReturnForbiddenWithoutLoadingTheTest()
    {
        // Arrange
        StubEnrolled(false);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenError>();
        await _lessonRepository.DidNotReceive()
            .GetTestLessonInCourseAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTestLessonIsNotVisibleInTheCourse_ShouldReturnNotFound()
    {
        // Arrange
        StubTestLesson(null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
    }

    // Idempotency

    [Fact]
    public async Task Handle_WhenAnAttemptIsAlreadyInProgress_ShouldReturnItWithoutCreatingAnother()
    {
        // Arrange — the multi-tab case: both tabs must land on the same attempt
        var existing = TestAttempt.Create(CourseId, LessonId, VersionId, StudentId, attemptNumber: 2);
        StubInProgressAttempt(existing);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AttemptId.Should().Be(existing.Id);
        result.Value.AttemptNumber.Should().Be(2);

        await _testAttemptRepository.DidNotReceive()
            .AddAsync(Arg.Any<TestAttempt>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Attempt limit

    [Fact]
    public async Task Handle_WhenAttemptLimitIsReached_ShouldReturnForbidden()
    {
        // Arrange — 3 submitted attempts against a limit of 3
        StubTestLesson(Test(attemptLimit: 3));
        StubSubmittedAttempts(Submitted(1), Submitted(2), Submitted(3));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenError>();
        await _testAttemptRepository.DidNotReceive()
            .AddAsync(Arg.Any<TestAttempt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOneAttemptRemainsUnderTheLimit_ShouldStartIt()
    {
        // Arrange
        StubTestLesson(Test(attemptLimit: 3));
        StubSubmittedAttempts(Submitted(1), Submitted(2));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AttemptNumber.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenAttemptLimitIsNotSet_ShouldAllowUnlimitedRetakes()
    {
        // Arrange
        StubTestLesson(Test(attemptLimit: null));
        StubSubmittedAttempts(Submitted(1), Submitted(2), Submitted(3), Submitted(4), Submitted(5));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AttemptNumber.Should().Be(6);
    }

    // Cooldown

    [Fact]
    public async Task Handle_WhenCooldownHasNotElapsed_ShouldReturnForbiddenWithRemainingMinutes()
    {
        // Arrange — submitted 10 minutes ago, 60-minute cooldown → ~50 minutes left
        StubTestLesson(Test(cooldownMinutes: 60));
        StubSubmittedAttempts(Submitted(1, submittedAt: DateTime.UtcNow.AddMinutes(-10)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors.Should().ContainSingle().Which;
        error.Should().BeOfType<ForbiddenError>();
        error.Message.Should().Contain("50");
    }

    [Fact]
    public async Task Handle_WhenCooldownHasElapsed_ShouldStartTheNextAttempt()
    {
        // Arrange — submitted 61 minutes ago, 60-minute cooldown
        StubTestLesson(Test(cooldownMinutes: 60));
        StubSubmittedAttempts(Submitted(1, submittedAt: DateTime.UtcNow.AddMinutes(-61)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AttemptNumber.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenCooldownIsMeasuredFromTheHighestNumberedAttempt_ShouldIgnoreOlderOnes()
    {
        // Arrange — the spec orders by AttemptNumber descending, so attempt 2 governs the cooldown
        StubTestLesson(Test(cooldownMinutes: 30));
        StubSubmittedAttempts(
            Submitted(2, submittedAt: DateTime.UtcNow.AddMinutes(-5)),
            Submitted(1, submittedAt: DateTime.UtcNow.AddDays(-1)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenError>();
    }

    [Fact]
    public async Task Handle_WhenCooldownIsSetButNoAttemptWasSubmittedYet_ShouldStartTheFirstAttempt()
    {
        // Arrange
        StubTestLesson(Test(cooldownMinutes: 60));
        StubSubmittedAttempts();

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AttemptNumber.Should().Be(1);
    }

    // Persistence

    [Fact]
    public async Task Handle_WhenNoPreviousAttemptsExist_ShouldPersistAttemptNumberOne()
    {
        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AttemptNumber.Should().Be(1);

        await _testAttemptRepository.Received(1).AddAsync(
            Arg.Is<TestAttempt>(a =>
                a.CourseId == CourseId && a.TestLessonId == LessonId &&
                a.TestVersionId == VersionId &&
                a.StudentId == StudentId && a.AttemptNumber == 1 && !a.IsSubmitted),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAConcurrentRequestAlreadyInsertedAnAttempt_ShouldReturnThatAttemptInsteadOfThrowing()
    {
        // Arrange — the unique index rejects our insert; the winner's attempt is now visible
        var winner = TestAttempt.Create(CourseId, LessonId, VersionId, StudentId, attemptNumber: 1);

        _testAttemptRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<TestAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(_ => null, _ => winner);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new InvalidOperationException("duplicate key"));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AttemptId.Should().Be(winner.Id);
    }

    [Fact]
    public async Task Handle_WhenInsertFailsForAnUnrelatedReason_ShouldRethrow()
    {
        // Arrange — nothing was inserted concurrently, so the failure is real and must not be swallowed
        StubInProgressAttempt(null);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new InvalidOperationException("connection reset"));

        // Act
        var act = () => _sut.Handle(Command, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("connection reset");
    }

    // Fixtures

    private static TestLesson Test(int? attemptLimit = null, int? cooldownMinutes = null)
    {
        var lesson = TestLesson.Create(
            Guid.NewGuid(), "Quiz", attemptLimit: attemptLimit, cooldownMinutes: cooldownMinutes);

        lesson.SetCurrentVersion(Version(lesson.Id));

        return lesson;
    }

    /// <summary>
    /// A version whose id is the fixed <see cref="VersionId"/>, so assertions can name the version an
    /// attempt should have been pinned to.
    /// </summary>
    private static TestVersion Version(Guid lessonId)
    {
        var version = TestVersion.Create(lessonId, [
            new QuestionBlueprint(
                "Question",
                Domain.Enums.QuestionType.SingleChoice,
                [new QuestionOptionBlueprint("Wrong", false), new QuestionOptionBlueprint("Right", true)],
                null)
        ]);

        typeof(Domain.Common.BaseEntity)
            .GetProperty(nameof(TestVersion.Id))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(version, [VersionId]);

        return version;
    }

    private static TestAttempt Submitted(int attemptNumber, DateTime? submittedAt = null)
    {
        var attempt = TestAttempt.Create(CourseId, LessonId, VersionId, StudentId, attemptNumber);
        attempt.Submit([], score: 1, maxScore: 1, passingThreshold: 70);

        if (submittedAt.HasValue)
        {
            // Submit() stamps SubmittedAt with UtcNow; there is no domain method to backdate it.
            typeof(TestAttempt)
                .GetProperty(nameof(TestAttempt.SubmittedAt))!
                .GetSetMethod(nonPublic: true)!
                .Invoke(attempt, [submittedAt.Value]);
        }

        return attempt;
    }

    private void StubEnrolled(bool enrolled) =>
        _enrollmentRepository
            .AnyAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(enrolled);

    private void StubTestLesson(TestLesson? lesson) =>
        _lessonRepository
            .GetTestLessonInCourseAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(lesson);

    private void StubInProgressAttempt(TestAttempt? attempt) =>
        _testAttemptRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<TestAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(attempt);

    /// <summary>Submitted attempts, in the order the specification returns them (highest number first).</summary>
    private void StubSubmittedAttempts(params TestAttempt[] attempts) =>
        _testAttemptRepository
            .ListAsync(Arg.Any<ISpecification<TestAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(attempts.ToList());
}
