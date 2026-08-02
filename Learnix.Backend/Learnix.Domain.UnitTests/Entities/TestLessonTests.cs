using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Domain.UnitTests.Entities;

public class TestLessonTests
{
    private static TestLesson Create()
        => TestLesson.Create(Guid.NewGuid(), "Lesson");

    private static QuestionBlueprint Blueprint(string text = "Question?") => new(
        text,
        QuestionType.SingleChoice,
        [
            new QuestionOptionBlueprint("A", true),
            new QuestionOptionBlueprint("B", false)
        ],
        null);

    [Fact]
    public void Create_ShouldHaveNoCurrentVersion()
    {
        // Act
        var lesson = Create();

        // Assert
        lesson.CurrentVersionId.Should().BeNull();
        lesson.QuestionsCount.Should().Be(0);
        lesson.IsPublishReady().Should().BeFalse();
    }

    [Fact]
    public void SetCurrentVersion_ShouldPointAtTheVersionAndCountItsQuestions()
    {
        // Arrange
        var lesson = Create();
        var version = TestVersion.Create(lesson.Id, [Blueprint(), Blueprint("Another?")]);

        // Act
        lesson.SetCurrentVersion(version);

        // Assert
        lesson.CurrentVersionId.Should().Be(version.Id);
        lesson.QuestionsCount.Should().Be(2);
        lesson.IsPublishReady().Should().BeTrue();
    }

    [Fact]
    public void SetCurrentVersion_WhenVersionBelongsToAnotherLesson_ShouldThrow()
    {
        // Arrange
        var lesson = Create();
        var foreignVersion = TestVersion.Create(Guid.NewGuid(), [Blueprint()]);

        // Act
        var act = () => lesson.SetCurrentVersion(foreignVersion);

        // Assert
        act.Should().Throw<DomainException>();
    }
}
