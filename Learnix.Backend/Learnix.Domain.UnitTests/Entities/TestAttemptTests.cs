using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.TestAttempts;
using Learnix.Domain.ValueObjects;

namespace Learnix.Domain.UnitTests.Entities;

public class TestAttemptTests
{
    private const int PassingThreshold = 60;

    private static TestAttempt Started()
        => TestAttempt.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), attemptNumber: 1);

    private static List<StudentAnswer> Answers() =>
    [
        new StudentAnswer(0, [2], null),
        new StudentAnswer(1, [0], null),
    ];

    // Creation
    // ========
    [Fact]
    public void Create_ShouldStartUnsubmittedWithNoAnswers()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        // Act
        var attempt = TestAttempt.Create(courseId, lessonId, studentId, attemptNumber: 3);

        // Assert
        attempt.CourseId.Should().Be(courseId);
        attempt.TestLessonId.Should().Be(lessonId);
        attempt.StudentId.Should().Be(studentId);
        attempt.AttemptNumber.Should().Be(3);
        attempt.IsSubmitted.Should().BeFalse();
        attempt.SubmittedAt.Should().BeNull();
        attempt.Score.Should().BeNull();
        attempt.MaxScore.Should().BeNull();
        attempt.Passed.Should().BeNull();
        attempt.Answers.Should().BeEmpty();
        attempt.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Submit — happy path
    // ==================
    [Fact]
    public void Submit_ShouldRecordAnswersScoreAndSubmissionTime()
    {
        // Arrange
        var attempt = Started();
        var answers = Answers();

        // Act
        attempt.Submit(answers, score: 2, maxScore: 2, PassingThreshold);

        // Assert
        attempt.IsSubmitted.Should().BeTrue();
        attempt.SubmittedAt.Should().NotBeNull();
        attempt.Score.Should().Be(2);
        attempt.MaxScore.Should().Be(2);
        attempt.Answers.Should().BeEquivalentTo(answers);
    }

    [Fact]
    public void Submit_ShouldRaiseTestSubmittedEventCarryingQuestionCountAndOutcome()
    {
        // Arrange
        var attempt = Started();

        // Act
        attempt.Submit(Answers(), score: 2, maxScore: 2, PassingThreshold);

        // Assert — QuestionsCount is fed from maxScore: one question is worth one point
        var @event = attempt.DomainEvents.OfType<TestSubmittedDomainEvent>().Should().ContainSingle().Subject;
        @event.StudentId.Should().Be(attempt.StudentId);
        @event.TestLessonId.Should().Be(attempt.TestLessonId);
        @event.AttemptId.Should().Be(attempt.Id);
        @event.QuestionsCount.Should().Be(2);
        @event.Passed.Should().BeTrue();
        @event.DurationSeconds.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Submit_ShouldReplaceAnyPreviouslyStagedAnswers()
    {
        // Arrange — the collection handed to Submit is the single source of truth for Answers
        var attempt = Started();
        var answers = Answers();

        // Act
        attempt.Submit(answers, score: 1, maxScore: 2, PassingThreshold);
        answers.Add(new StudentAnswer(2, [1], null));

        // Assert — mutating the caller's list afterwards must not leak into the attempt
        attempt.Answers.Should().HaveCount(2);
    }

    // Submit-once invariant
    // ====================
    [Fact]
    public void Submit_WhenAlreadySubmitted_ShouldThrowDomainException()
    {
        // Arrange
        var attempt = Started();
        attempt.Submit(Answers(), score: 1, maxScore: 2, PassingThreshold);

        // Act
        var act = () => attempt.Submit(Answers(), score: 2, maxScore: 2, PassingThreshold);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Test attempt has already been submitted.");
    }

    [Fact]
    public void Submit_WhenAlreadySubmitted_ShouldNotOverwriteTheOriginalScore()
    {
        // Arrange
        var attempt = Started();
        attempt.Submit(Answers(), score: 1, maxScore: 2, PassingThreshold);

        // Act
        var act = () => attempt.Submit(Answers(), score: 2, maxScore: 2, PassingThreshold);

        // Assert — a rejected resubmit leaves the recorded result untouched
        act.Should().Throw<DomainException>();
        attempt.Score.Should().Be(1);
    }

    // Score validation
    // ================
    [Theory]
    [InlineData(-1, 2)]   // negative score
    [InlineData(1, -2)]   // negative max
    [InlineData(3, 2)]    // score above max
    public void Submit_WhenScoreIsInvalid_ShouldThrowDomainException(int score, int maxScore)
    {
        // Arrange
        var attempt = Started();

        // Act
        var act = () => attempt.Submit(Answers(), score, maxScore, PassingThreshold);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Invalid test score.");
        attempt.IsSubmitted.Should().BeFalse();
    }

    // Pass / fail threshold
    // ====================
    [Theory]
    [InlineData(1, 2, 50, true)]    // exactly on the threshold passes
    [InlineData(1, 2, 51, false)]   // one point below fails
    [InlineData(2, 2, 100, true)]   // a perfect score meets a 100% threshold
    [InlineData(0, 2, 1, false)]    // zero never passes a non-zero threshold
    public void Submit_ShouldDecidePassedByPercentageAgainstThreshold(
        int score, int maxScore, int threshold, bool expected)
    {
        // Arrange
        var attempt = Started();

        // Act
        attempt.Submit(Answers(), score, maxScore, threshold);

        // Assert
        attempt.Passed.Should().Be(expected);
    }

    [Fact]
    public void Submit_ShouldRoundPercentageAwayFromZero()
    {
        // Arrange — 1/3 is 33.33%, 2/3 is 66.67% → rounds up to 67 and clears a 67% threshold
        var attempt = Started();

        // Act
        attempt.Submit(Answers(), score: 2, maxScore: 3, passingThreshold: 67);

        // Assert
        attempt.Passed.Should().BeTrue();
    }

    [Fact]
    public void Submit_WhenTestHasNoQuestions_ShouldScoreZeroPercentAndNotThrow()
    {
        // Arrange — maxScore 0 would divide by zero if the guard were missing
        var attempt = Started();

        // Act
        attempt.Submit([], score: 0, maxScore: 0, PassingThreshold);

        // Assert
        attempt.Passed.Should().BeFalse();
        attempt.Score.Should().Be(0);
        attempt.MaxScore.Should().Be(0);
    }
}
