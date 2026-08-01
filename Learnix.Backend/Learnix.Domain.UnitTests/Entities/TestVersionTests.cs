using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Domain.UnitTests.Entities;

public class TestVersionTests
{
    private static readonly Guid LessonId = Guid.NewGuid();

    private static QuestionBlueprint Choice(string text, params (string Text, bool IsCorrect)[] options) =>
        new(text, QuestionType.SingleChoice, [.. options.Select(o => new QuestionOptionBlueprint(o.Text, o.IsCorrect))], null);

    private static QuestionBlueprint Text(string text, string answer) =>
        new(text, QuestionType.TextInput, null, new TextAnswerBlueprint(answer, IgnoreCase: true, AllowFuzzy: false));

    private static List<QuestionBlueprint> TwoQuestions() =>
    [
        Choice("Capital of France?", ("Paris", true), ("Rome", false)),
        Text("Two plus two?", "4")
    ];

    // Creation
    // ========
    [Fact]
    public void Create_ShouldStartAtVersionOneAndBuildQuestionsInOrder()
    {
        // Act
        var version = TestVersion.Create(LessonId, TwoQuestions());

        // Assert
        version.TestLessonId.Should().Be(LessonId);
        version.VersionNumber.Should().Be(1);
        version.MaxScore.Should().Be(2);
        version.Questions.Select(q => q.Order).Should().Equal(0, 1);
        version.Questions[0].Options.Select(o => o.Order).Should().Equal(0, 1);
    }

    [Fact]
    public void Create_WithNoQuestions_ShouldThrow()
    {
        // Act
        var act = () => TestVersion.Create(LessonId, []);

        // Assert
        act.Should().Throw<DomainException>();
    }

    // Branch — the edit that has attempts to protect
    // =============================================
    [Fact]
    public void Branch_ShouldProduceTheNextVersionOfTheSameLessonAndLeaveThisOneAlone()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, TwoQuestions());

        // Act
        var next = version.Branch([Choice("Something else?", ("A", true), ("B", false))]);

        // Assert
        next.TestLessonId.Should().Be(LessonId);
        next.VersionNumber.Should().Be(2);
        next.Id.Should().NotBe(version.Id);
        next.Questions.Should().ContainSingle();

        // The whole point: the version students already sat is untouched.
        version.VersionNumber.Should().Be(1);
        version.Questions.Should().HaveCount(2);
    }

    [Fact]
    public void Overwrite_ShouldReplaceTheQuestionsAndKeepTheVersionNumber()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, TwoQuestions());

        // Act
        version.Overwrite([Choice("Only one now?", ("A", true), ("B", false))]);

        // Assert
        version.VersionNumber.Should().Be(1);
        version.Questions.Should().ContainSingle();
        version.MaxScore.Should().Be(1);
    }

    // Matches — what decides whether a save touched the questions at all
    // =================================================================
    [Fact]
    public void Matches_WithTheSameBlueprints_ShouldBeTrue()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, TwoQuestions());

        // Act & Assert
        version.Matches(TwoQuestions()).Should().BeTrue();
    }

    [Fact]
    public void Matches_WhenQuestionsAreReordered_ShouldBeFalse()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, TwoQuestions());
        var reordered = TwoQuestions();
        reordered.Reverse();

        // Act & Assert
        version.Matches(reordered).Should().BeFalse();
    }

    [Fact]
    public void Matches_WhenOptionsAreReordered_ShouldBeFalse()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, [Choice("Q?", ("A", true), ("B", false))]);

        // Act & Assert
        version.Matches([Choice("Q?", ("B", false), ("A", true))]).Should().BeFalse();
    }

    [Fact]
    public void Matches_WhenOnlyTheCorrectAnswerMoved_ShouldBeFalse()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, [Choice("Q?", ("A", true), ("B", false))]);

        // Act & Assert
        version.Matches([Choice("Q?", ("A", false), ("B", true))]).Should().BeFalse();
    }

    [Fact]
    public void Matches_WhenAQuestionIsRemoved_ShouldBeFalse()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, TwoQuestions());

        // Act & Assert
        version.Matches([TwoQuestions()[0]]).Should().BeFalse();
    }

    [Fact]
    public void Matches_WhenTheTextAnswerConfigChanges_ShouldBeFalse()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, [Text("Two plus two?", "4")]);

        // Act & Assert
        version.Matches([
            new QuestionBlueprint(
                "Two plus two?",
                QuestionType.TextInput,
                null,
                new TextAnswerBlueprint("4", IgnoreCase: true, AllowFuzzy: true))
        ]).Should().BeFalse();
    }

    // Scoring
    // =======
    [Fact]
    public void Score_ShouldCountOnlyTheQuestionsAnsweredCorrectly()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, TwoQuestions());

        var answers = new List<StudentAnswer>
        {
            new(0, [0], null),     // Paris — correct
            new(1, [], "five")     // wrong
        };

        // Act & Assert
        version.Score(answers).Should().Be(1);
    }

    [Fact]
    public void Score_WhenAQuestionIsUnanswered_ShouldCountItWrong()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, TwoQuestions());

        // Act & Assert
        version.Score([new StudentAnswer(0, [0], null)]).Should().Be(1);
    }

    [Fact]
    public void Score_WithAnswersForQuestionsThisVersionDoesNotHave_ShouldIgnoreThem()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, [Choice("Q?", ("A", true), ("B", false))]);

        // Act & Assert
        version.Score([new StudentAnswer(0, [0], null), new StudentAnswer(7, [0], null)]).Should().Be(1);
    }

    [Fact]
    public void Score_WithADuplicatedQuestionOrder_ShouldNotThrow()
    {
        // Arrange
        var version = TestVersion.Create(LessonId, [Choice("Q?", ("A", true), ("B", false))]);

        // Act
        var score = version.Score([new StudentAnswer(0, [0], null), new StudentAnswer(0, [1], null)]);

        // Assert — the first answer stands; rejecting the request is the validator's job, not the scorer's.
        score.Should().Be(1);
    }
}
