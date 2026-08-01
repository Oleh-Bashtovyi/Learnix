using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.Lessons.Commands.UpdateTestLesson;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Application.UnitTests.Lessons.Commands.UpdateTestLesson;

/// <summary>
/// The rule this handler exists to enforce (ADR-BACK-LMS-006): an edit reuses the current version while
/// nothing has been attempted against it, and branches a new one the moment something has.
/// </summary>
public class UpdateTestLessonCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly ILessonRepository _lessonRepository = Substitute.For<ILessonRepository>();
    private readonly ITestVersionRepository _testVersionRepository = Substitute.For<ITestVersionRepository>();
    private readonly ITestAttemptRepository _testAttemptRepository = Substitute.For<ITestAttemptRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateTestLessonCommandHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();

    private readonly Course _course = Course.Create(InstructorId, Guid.NewGuid(), "React", "Learn React", 49m);
    private readonly TestLesson _lesson;
    private readonly TestVersion _currentVersion;

    public UpdateTestLessonCommandHandlerTests()
    {
        _sut = new UpdateTestLessonCommandHandler(
            _courseRepository, _lessonRepository, _testVersionRepository,
            _testAttemptRepository, _unitOfWork, _currentUser);

        var sectionId = _course.AddSection("Section 1").Id;

        _lesson = TestLesson.Create(sectionId, "Quiz");
        _currentVersion = TestVersion.Create(_lesson.Id, [Choice("Capital of France?", ("Paris", true), ("Rome", false))]);
        _lesson.SetCurrentVersion(_currentVersion);

        _currentUser.UserId.Returns(InstructorId);

        _courseRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(_course);

        _lessonRepository
            .GetLessonOfTypeByIdAsync<TestLesson>(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_lesson);

        _testVersionRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<TestVersion>>(), Arg.Any<CancellationToken>())
            .Returns(_currentVersion);

        StubAttemptsExist(false);
    }

    // Guards

    [Fact]
    public async Task Handle_WhenLessonDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        _lessonRepository
            .GetLessonOfTypeByIdAsync<TestLesson>(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((TestLesson?)null);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Settings only

    [Fact]
    public async Task Handle_WhenOnlyTheSettingsChanged_ShouldLeaveTheVersionAlone()
    {
        // Arrange — the same questions, a new title and threshold
        var command = Command(title: "Renamed", passingThreshold: 90);

        // Act
        var result = await _sut.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _lesson.Title.Should().Be("Renamed");
        _lesson.PassingThreshold.Should().Be(90);
        _lesson.CurrentVersionId.Should().Be(_currentVersion.Id);
        _currentVersion.VersionNumber.Should().Be(1);

        _testVersionRepository.DidNotReceive().Add(Arg.Any<TestVersion>());

        // Not even asked: there is nothing to protect from a save that did not touch the questions.
        await _testAttemptRepository.DidNotReceive()
            .AnyAsync(Arg.Any<ISpecification<TestAttempt>>(), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Questions changed, nothing attempted → reuse the row

    [Fact]
    public async Task Handle_WhenQuestionsChangedAndNothingWasAttempted_ShouldOverwriteTheSameVersion()
    {
        // Arrange
        var command = Command(questions: [
            Choice("Capital of Italy?", ("Rome", true), ("Paris", false)),
            Choice("Capital of Spain?", ("Madrid", true), ("Lisbon", false))
        ]);

        // Act
        var result = await _sut.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _currentVersion.VersionNumber.Should().Be(1);
        _currentVersion.Questions.Should().HaveCount(2);
        _currentVersion.Questions[0].Text.Should().Be("Capital of Italy?");

        _lesson.CurrentVersionId.Should().Be(_currentVersion.Id);
        _lesson.QuestionsCount.Should().Be(2);

        _testVersionRepository.DidNotReceive().Add(Arg.Any<TestVersion>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Questions changed, something was attempted → branch

    [Fact]
    public async Task Handle_WhenQuestionsChangedAndTheVersionHasAttempts_ShouldBranchAndLeaveTheOldVersionIntact()
    {
        // Arrange
        StubAttemptsExist(true);

        var command = Command(questions: [Choice("Capital of Italy?", ("Rome", true), ("Paris", false))]);

        // Act
        var result = await _sut.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // The edition students already sat is byte-for-byte what it was.
        _currentVersion.VersionNumber.Should().Be(1);
        _currentVersion.Questions.Should().ContainSingle()
            .Which.Text.Should().Be("Capital of France?");

        _testVersionRepository.Received(1).Add(
            Arg.Is<TestVersion>(v =>
                v.TestLessonId == _lesson.Id &&
                v.VersionNumber == 2 &&
                v.Questions.Count == 1));

        _lesson.CurrentVersionId.Should().NotBe(_currentVersion.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOnlyTheQuestionOrderChangedAndTheVersionHasAttempts_ShouldStillBranch()
    {
        // Arrange — reordering is exactly the edit that used to silently rewrite submitted answers
        StubAttemptsExist(true);

        var command = Command(questions: [
            Choice("Capital of France?", ("Rome", false), ("Paris", true))
        ]);

        // Act
        var result = await _sut.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _currentVersion.Questions[0].Options[0].Text.Should().Be("Paris");

        _testVersionRepository.Received(1).Add(Arg.Is<TestVersion>(v => v.VersionNumber == 2));
    }

    // Recovery

    [Fact]
    public async Task Handle_WhenTheLessonHasNoVersionAtAll_ShouldCreateTheFirstOne()
    {
        // Arrange — a lesson that somehow never got a version still has to be editable
        var orphan = TestLesson.Create(_course.Sections.Single().Id, "Quiz");

        _lessonRepository
            .GetLessonOfTypeByIdAsync<TestLesson>(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(orphan);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        orphan.CurrentVersionId.Should().NotBeNull();

        _testVersionRepository.Received(1).Add(
            Arg.Is<TestVersion>(v => v.TestLessonId == orphan.Id && v.VersionNumber == 1));
    }

    // Fixtures

    private static QuestionBlueprint Choice(string text, params (string Text, bool IsCorrect)[] options) =>
        new(text, QuestionType.SingleChoice,
            [.. options.Select(o => new QuestionOptionBlueprint(o.Text, o.IsCorrect))], null);

    private UpdateTestLessonCommand Command(
        string title = "Quiz",
        int passingThreshold = 70,
        IReadOnlyList<QuestionBlueprint>? questions = null) =>
        new(_course.Id, _lesson.Id, title, null, null, null, passingThreshold, TestReviewMode.FullReview,
            questions ?? [Choice("Capital of France?", ("Paris", true), ("Rome", false))]);

    private void StubAttemptsExist(bool exist) =>
        _testAttemptRepository
            .AnyAsync(Arg.Any<ISpecification<TestAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(exist);
}
