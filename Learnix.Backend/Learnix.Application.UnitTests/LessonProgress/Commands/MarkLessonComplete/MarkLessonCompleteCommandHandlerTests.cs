using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.LessonProgress.Commands.MarkLessonComplete;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.UnitTests.LessonProgress.Commands.MarkLessonComplete;

public class MarkLessonCompleteCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ILessonRepository _lessonRepository = Substitute.For<ILessonRepository>();
    private readonly ILessonProgressRepository _lessonProgressRepository = Substitute.For<ILessonProgressRepository>();
    private readonly ICourseCompletionService _courseCompletion = Substitute.For<ICourseCompletionService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly MarkLessonCompleteCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid LessonId = Guid.NewGuid();

    public MarkLessonCompleteCommandHandlerTests()
    {
        _sut = new MarkLessonCompleteCommandHandler(
            _currentUser,
            _enrollmentRepository,
            _lessonRepository,
            _lessonProgressRepository,
            _courseCompletion,
            _unitOfWork);

        _currentUser.UserId.Returns(StudentId);
        StubEnrolled(true);
        StubLesson(PostLesson.Create(Guid.NewGuid(), "Lesson", "content"));
    }

    // Guards

    [Fact]
    public async Task Handle_WhenNotEnrolled_ShouldReturnForbidden()
    {
        // Arrange
        StubEnrolled(false);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenError>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLessonIsNotVisibleInTheCourse_ShouldReturnNotFound()
    {
        // Arrange
        StubLesson(null);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Handle_WhenLessonIsATestWithQuestions_ShouldReturnForbidden()
    {
        // Arrange — a quiz is completed by submitting it, never by ticking it off
        var test = TestLesson.Create(Guid.NewGuid(), "Quiz", passingThreshold: 70);
        test.SetCurrentVersion(TestVersion.Create(test.Id, [
            new QuestionBlueprint(
                "Q",
                QuestionType.SingleChoice,
                [new QuestionOptionBlueprint("Wrong", false), new QuestionOptionBlueprint("Right", true)],
                null)
        ]));
        StubLesson(test);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenError>();
    }

    // Progress

    [Fact]
    public async Task Handle_WhenNoProgressExists_ShouldStageACompletedRowWithoutSavingItSeparately()
    {
        // Arrange
        StubProgress(null);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert — `AddAsync` would commit on its own, splitting the lesson from the certificate it
        // earns; the row must be staged and committed with everything else
        result.IsSuccess.Should().BeTrue();
        _lessonProgressRepository.Received(1).Add(
            Arg.Is<LessonProgressEntity>(p => p.IsCompleted && p.CourseId == CourseId && p.LessonId == LessonId));
        await _lessonProgressRepository.DidNotReceive()
            .AddAsync(Arg.Any<LessonProgressEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIncompleteProgressExists_ShouldCompleteItWithoutAddingAnother()
    {
        // Arrange
        var progress = LessonProgressEntity.Create(CourseId, LessonId, StudentId);
        StubProgress(progress);

        // Act
        await _sut.Handle(Command(), default);

        // Assert
        progress.IsCompleted.Should().BeTrue();
        _lessonProgressRepository.DidNotReceive().Add(Arg.Any<LessonProgressEntity>());
    }

    // Course completion

    [Fact]
    public async Task Handle_WhenLessonIsCompletedNow_ShouldEvaluateCourseCompletionNamingThatLesson()
    {
        // Arrange
        StubProgress(null);

        // Act
        await _sut.Handle(Command(), default);

        // Assert — naming the lesson is what frees the check from having to know whether its row was
        // already flushed
        await _courseCompletion.Received(1).TryCompleteAsync(
            StudentId, CourseId, LessonId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLessonWasAlreadyCompleted_ShouldNotReEvaluateCourseCompletion()
    {
        // Arrange — ticking a finished lesson again must not issue a second certificate
        var progress = LessonProgressEntity.Create(CourseId, LessonId, StudentId);
        progress.MarkCompleted();
        StubProgress(progress);

        // Act
        await _sut.Handle(Command(), default);

        // Assert
        await _courseCompletion.DidNotReceive().TryCompleteAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    // Fixtures

    private static MarkLessonCompleteCommand Command() => new(CourseId, LessonId);

    private void StubEnrolled(bool enrolled) =>
        _enrollmentRepository
            .AnyAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(enrolled);

    private void StubLesson(Lesson? lesson) =>
        _lessonRepository
            .GetVisibleLessonInCourseAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(lesson);

    private void StubProgress(LessonProgressEntity? progress) =>
        _lessonProgressRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<LessonProgressEntity>>(), Arg.Any<CancellationToken>())
            .Returns(progress);
}
