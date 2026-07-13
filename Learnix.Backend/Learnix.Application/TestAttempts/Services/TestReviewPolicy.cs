using Learnix.Domain.Enums;

namespace Learnix.Application.TestAttempts.Services;

/// <summary>
/// The single reading of <see cref="TestReviewMode"/>, shared by everything that can expose a
/// submitted attempt: the response to the submission, the review of a past attempt, and the tutor.
/// <para>
/// It lives in one place on purpose. Three call sites each deciding for themselves what a mode means
/// is three chances to disagree, and a disagreement here is a leak: the instructor hides the correct
/// answers, and the AI tutor — or the results screen — recites them anyway.
/// </para>
/// </summary>
public static class TestReviewPolicy
{
    /// <summary>Whether the student may see the questions and what they themselves answered.</summary>
    public static bool ShowsAnswers(TestReviewMode mode) => mode >= TestReviewMode.AnswersOnly;

    /// <summary>Whether the student may see which questions they got wrong.</summary>
    public static bool ShowsCorrectness(TestReviewMode mode) => mode >= TestReviewMode.AnswersAndCorrectness;

    /// <summary>Whether the student may see what the right answer actually was.</summary>
    public static bool ShowsCorrectAnswers(TestReviewMode mode) => mode >= TestReviewMode.FullReview;
}
