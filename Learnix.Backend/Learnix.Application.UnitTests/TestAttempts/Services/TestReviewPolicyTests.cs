using Learnix.Application.TestAttempts.Services;
using Learnix.Domain.Enums;

namespace Learnix.Application.UnitTests.TestAttempts.Services;

public class TestReviewPolicyTests
{
    [Theory]
    [InlineData(TestReviewMode.ScoreOnly, false, false, false)]
    [InlineData(TestReviewMode.AnswersOnly, true, false, false)]
    [InlineData(TestReviewMode.AnswersAndCorrectness, true, true, false)]
    [InlineData(TestReviewMode.FullReview, true, true, true)]
    public void Policy_ShouldDiscloseExactlyWhatTheModePromises(
        TestReviewMode mode,
        bool answers,
        bool correctness,
        bool correctAnswers)
    {
        // Act + Assert
        TestReviewPolicy.ShowsAnswers(mode).Should().Be(answers);
        TestReviewPolicy.ShowsCorrectness(mode).Should().Be(correctness);
        TestReviewPolicy.ShowsCorrectAnswers(mode).Should().Be(correctAnswers);
    }

    /// <summary>
    /// The modes are a ladder, and the gates read as `mode >= X`. If a future value is inserted in the
    /// middle with the wrong number, that comparison silently starts disclosing more than intended —
    /// so pin the order rather than trusting it.
    /// </summary>
    [Fact]
    public void Modes_ShouldBeOrderedByHowMuchTheyDisclose()
    {
        // Assert
        ((int)TestReviewMode.ScoreOnly).Should().BeLessThan((int)TestReviewMode.AnswersOnly);
        ((int)TestReviewMode.AnswersOnly).Should().BeLessThan((int)TestReviewMode.AnswersAndCorrectness);
        ((int)TestReviewMode.AnswersAndCorrectness).Should().BeLessThan((int)TestReviewMode.FullReview);
    }
}
