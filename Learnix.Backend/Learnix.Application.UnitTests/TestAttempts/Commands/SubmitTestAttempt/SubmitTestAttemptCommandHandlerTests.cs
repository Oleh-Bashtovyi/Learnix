using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.UnitTests.TestAttempts.Commands.SubmitTestAttempt;

public class SubmitTestAttemptCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILessonRepository _lessonRepository = Substitute.For<ILessonRepository>();
    private readonly ILessonProgressRepository _lessonProgressRepository = Substitute.For<ILessonProgressRepository>();
    private readonly ITestAttemptRepository _testAttemptRepository = Substitute.For<ITestAttemptRepository>();
    private readonly ICourseCompletionService _courseCompletion = Substitute.For<ICourseCompletionService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SubmitTestAttemptCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid LessonId = Guid.NewGuid();

    public SubmitTestAttemptCommandHandlerTests()
    {
        _sut = new SubmitTestAttemptCommandHandler(
            _currentUser,
            _lessonRepository,
            _lessonProgressRepository,
            _testAttemptRepository,
            _courseCompletion,
            _unitOfWork);

        _currentUser.UserId.Returns(StudentId);
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
    public async Task Handle_WhenAttemptDoesNotBelongToStudent_ShouldReturnNotFound()
    {
        // Arrange — the specification filters by student, so a foreign attempt simply is not found
        StubAttempt(null);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAttemptWasAlreadySubmitted_ShouldReturnConflict()
    {
        // Arrange
        var lesson = TestWithThreeSingleChoiceQuestions();
        var attempt = NewAttempt();
        attempt.Submit([], score: 0, maxScore: 3, passingThreshold: 70);

        StubAttempt(attempt);
        StubTestLesson(lesson);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictError>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRouteIdsDoNotMatchTheAttempt_ShouldReturnNotFound()
    {
        // Arrange — attempt belongs to a different course than the one in the URL
        StubAttempt(TestAttempt.Create(Guid.NewGuid(), LessonId, StudentId, attemptNumber: 1));

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
        await _lessonRepository.DidNotReceive()
            .GetTestLessonInCourseAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTestLessonIsNotVisibleInTheCourse_ShouldReturnNotFound()
    {
        // Arrange
        StubAttempt(NewAttempt());
        StubTestLesson(null);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
    }

    // Scoring

    [Fact]
    public async Task Handle_WhenTwoOfThreeAnswersAreCorrect_ShouldScoreTwoAndFailBelowThreshold()
    {
        // Arrange — 2/3 = 67% < 70%
        StubAttempt(NewAttempt());
        StubTestLesson(TestWithThreeSingleChoiceQuestions(passingThreshold: 70));

        var command = Command(Answer(0, 1), Answer(1, 1), Answer(2, 0));

        // Act
        var result = await _sut.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(2);
        result.Value.MaxScore.Should().Be(3);
        result.Value.Passed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenScoreRoundsUpToTheThreshold_ShouldPass()
    {
        // Arrange — 2/3 = 66.67% rounds away from zero to 67%, which meets a 67% threshold
        StubAttempt(NewAttempt());
        StubTestLesson(TestWithThreeSingleChoiceQuestions(passingThreshold: 67));

        var command = Command(Answer(0, 1), Answer(1, 1), Answer(2, 0));

        // Act
        var result = await _sut.Handle(command, default);

        // Assert
        result.Value.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAQuestionIsUnanswered_ShouldMarkItIncorrectRatherThanThrow()
    {
        // Arrange — no answer submitted for question 2
        StubAttempt(NewAttempt());
        StubTestLesson(TestWithThreeSingleChoiceQuestions());

        var command = Command(Answer(0, 1), Answer(1, 1));

        // Act
        var result = await _sut.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(2);
        result.Value.QuestionResults.Single(q => q.QuestionOrder == 2).IsCorrect.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSubmitted_ShouldRevealCorrectAnswersForEveryQuestion()
    {
        // Arrange
        StubAttempt(NewAttempt());
        StubTestLesson(TestWithOneChoiceAndOneTextQuestion());

        var command = Command(Answer(0, 1), TextAnswer(1, "paris"));

        // Act
        var result = await _sut.Handle(command, default);

        // Assert
        var choice = result.Value.QuestionResults.Single(q => q.QuestionOrder == 0);
        choice.CorrectOptionOrders.Should().Equal(1);
        choice.CorrectTextAnswer.Should().BeNull();

        var text = result.Value.QuestionResults.Single(q => q.QuestionOrder == 1);
        text.CorrectOptionOrders.Should().BeNull();
        text.CorrectTextAnswer.Should().Be("Paris");
        text.IsCorrect.Should().BeTrue("the text answer is configured as case-insensitive");
    }

    // Lesson progress

    [Fact]
    public async Task Handle_WhenNoProgressExists_ShouldCreateCompletedProgress()
    {
        // Arrange
        StubAttempt(NewAttempt());
        StubTestLesson(TestWithThreeSingleChoiceQuestions());
        StubProgress(null);

        // Act
        await _sut.Handle(Command(), default);

        // Assert — staged, not saved: the progress row must commit together with everything the
        // completion triggers
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
        StubAttempt(NewAttempt());
        StubTestLesson(TestWithThreeSingleChoiceQuestions());
        StubProgress(progress);

        // Act
        await _sut.Handle(Command(), default);

        // Assert
        progress.IsCompleted.Should().BeTrue();
        _lessonProgressRepository.DidNotReceive().Add(Arg.Any<LessonProgressEntity>());
    }

    // Course completion

    [Fact]
    public async Task Handle_WhenLessonIsCompletedByThisSubmission_ShouldEvaluateCourseCompletion()
    {
        // Arrange
        StubAttempt(NewAttempt());
        StubTestLesson(TestWithThreeSingleChoiceQuestions());
        StubProgress(null);

        // Act
        await _sut.Handle(Command(), default);

        // Assert — the lesson being completed right now is named explicitly, so the service does not
        // have to guess whether its progress row was already flushed
        await _courseCompletion.Received(1).TryCompleteAsync(
            StudentId, CourseId, LessonId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLessonWasAlreadyCompleted_ShouldNotReEvaluateCourseCompletion()
    {
        // Arrange — retaking a test the student already passed must not re-issue a certificate
        var progress = LessonProgressEntity.Create(CourseId, LessonId, StudentId);
        progress.MarkCompleted();

        StubAttempt(NewAttempt());
        StubTestLesson(TestWithThreeSingleChoiceQuestions());
        StubProgress(progress);

        // Act
        await _sut.Handle(Command(), default);

        // Assert
        await _courseCompletion.DidNotReceive().TryCompleteAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    // Fixtures

    private static SubmitTestAttemptCommand Command(params SubmittedAnswerDto[] answers) =>
        new(CourseId, LessonId, Guid.NewGuid(), answers);

    private static SubmittedAnswerDto Answer(int questionOrder, params int[] selectedOptionOrders) =>
        new(questionOrder, [.. selectedOptionOrders], null);

    private static SubmittedAnswerDto TextAnswer(int questionOrder, string text) =>
        new(questionOrder, [], text);

    private static TestAttempt NewAttempt() =>
        TestAttempt.Create(CourseId, LessonId, StudentId, attemptNumber: 1);

    /// <summary>Three single-choice questions whose correct option is always order 1.</summary>
    private static TestLesson TestWithThreeSingleChoiceQuestions(int passingThreshold = 70)
    {
        var lesson = TestLesson.Create(
            Guid.NewGuid(), "Quiz", passingThreshold: passingThreshold);

        lesson.ReplaceQuestions(Enumerable.Range(0, 3)
            .Select(i => new QuestionBlueprint(
                $"Question {i}",
                QuestionType.SingleChoice,
                [new QuestionOptionBlueprint("Wrong", false), new QuestionOptionBlueprint("Right", true)],
                null))
            .ToList());

        return lesson;
    }

    private static TestLesson TestWithOneChoiceAndOneTextQuestion()
    {
        var lesson = TestLesson.Create(Guid.NewGuid(), "Quiz");

        lesson.ReplaceQuestions([
            new QuestionBlueprint(
                "Pick the right one",
                QuestionType.SingleChoice,
                [new QuestionOptionBlueprint("Wrong", false), new QuestionOptionBlueprint("Right", true)],
                null),
            new QuestionBlueprint(
                "Capital of France?",
                QuestionType.TextInput,
                null,
                new TextAnswerBlueprint("Paris", IgnoreCase: true, AllowFuzzy: false))
        ]);

        return lesson;
    }

    private void StubAttempt(TestAttempt? attempt) =>
        _testAttemptRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<TestAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(attempt);

    private void StubTestLesson(TestLesson? lesson) =>
        _lessonRepository
            .GetTestLessonInCourseAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(lesson);

    private void StubProgress(LessonProgressEntity? progress) =>
        _lessonProgressRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<LessonProgressEntity>>(), Arg.Any<CancellationToken>())
            .Returns(progress);
}
