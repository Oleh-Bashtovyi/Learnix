namespace Learnix.Domain.Enums;

/// <summary>
/// How much of a submitted attempt the student is allowed to see. Chosen by the instructor, per test.
/// <para>
/// The values form a ladder: each one discloses everything the one below it does, plus one thing more.
/// That is why this is a single enum rather than a set of flags — "show the correct answers but not
/// which questions were wrong" is not a policy anyone wants, and modelling it as independent booleans
/// would make such states representable. Callers gate on the order: <c>mode &gt;= AnswersAndCorrectness</c>.
/// </para>
/// <para>
/// The mode governs every path that can reveal an attempt — the response to the submission itself, the
/// later review of a past attempt, and what the AI tutor is given. Gating only the review would be
/// theatre: the student already saw the answers on the results screen, and could simply screenshot them.
/// </para>
/// </summary>
public enum TestReviewMode
{
    /// <summary>Score and pass/fail. The questions are not returned at all.</summary>
    ScoreOnly = 0,

    /// <summary>The questions, and what the student answered. Not whether any of it was right.</summary>
    AnswersOnly = 1,

    /// <summary>Also which questions were answered correctly — but not what the right answer was.</summary>
    AnswersAndCorrectness = 2,

    /// <summary>The whole attempt, correct answers included. The default, and the behaviour before the mode existed.</summary>
    FullReview = 3
}
