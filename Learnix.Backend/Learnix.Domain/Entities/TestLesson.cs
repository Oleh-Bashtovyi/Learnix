using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;

namespace Learnix.Domain.Entities;

public class TestLesson : Lesson
{
    private TestLesson() { }

    private TestLesson(
        Guid sectionId,
        string title,
        string? description,
        int? attemptLimit,
        int? cooldownMinutes,
        int passingThreshold,
        TestReviewMode reviewMode)
        : base(sectionId, title, LessonType.Test)
    {
        Description = description;
        AttemptLimit = attemptLimit;
        CooldownMinutes = cooldownMinutes;
        PassingThreshold = passingThreshold;
        ReviewMode = reviewMode;
    }

    public string? Description { get; private set; }
    public int? AttemptLimit { get; private set; }
    public int? CooldownMinutes { get; private set; }
    public int PassingThreshold { get; private set; }

    /// <summary>
    /// The <see cref="TestVersion"/> a student starting the test right now would be served.
    /// <para>
    /// The questions themselves live there and not here, because an attempt has to stay readable
    /// against the exact list it was taken against (ADR-BACK-LMS-006). Null only between constructing
    /// the lesson and saving its first version — <see cref="IsPublishReady"/> keeps such a lesson
    /// hidden.
    /// </para>
    /// </summary>
    public Guid? CurrentVersionId { get; private set; }

    /// <summary>
    /// How many questions <see cref="CurrentVersionId"/> holds, denormalised so that listing a course
    /// does not have to join the versions. Not a score: what an attempt was marked out of is frozen on
    /// the attempt.
    /// </summary>
    public int QuestionsCount { get; private set; }

    /// <summary>How much of a submitted attempt the student may see back. See <see cref="TestReviewMode"/>.</summary>
    public TestReviewMode ReviewMode { get; private set; } = TestReviewMode.FullReview;

    public static TestLesson Create(
        Guid sectionId, string title,
        string? description = null,
        int? attemptLimit = null,
        int? cooldownMinutes = null,
        int passingThreshold = LessonConstants.DefaultPassingThreshold,
        TestReviewMode reviewMode = TestReviewMode.FullReview)
        => new(sectionId, title, description, attemptLimit, cooldownMinutes, passingThreshold, reviewMode);

    /// <summary>
    /// Points the lesson at the version students should now be served, and keeps
    /// <see cref="QuestionsCount"/> in step with it.
    /// </summary>
    public void SetCurrentVersion(TestVersion version)
    {
        if (version.TestLessonId != Id)
            throw new DomainException("Test version belongs to a different test lesson.");

        CurrentVersionId = version.Id;
        QuestionsCount = version.Questions.Count;

        EvaluateVisibility();
    }

    /// <summary>
    /// Everything about the test except its questions — those are a version's, and which version a
    /// save lands on depends on whether the current one has attempts to protect.
    /// </summary>
    public void UpdateTest(
        string title,
        string? description,
        int? attemptLimit,
        int? cooldownMinutes,
        int passingThreshold,
        TestReviewMode reviewMode)
    {
        UpdateTitle(title);
        UpdateSettings(description, attemptLimit, cooldownMinutes, passingThreshold, reviewMode);
    }

    public void UpdateSettings(
        string? description, int? attemptLimit,
        int? cooldownMinutes, int passingThreshold,
        TestReviewMode reviewMode)
    {
        Description = description;
        AttemptLimit = attemptLimit;
        CooldownMinutes = cooldownMinutes;
        PassingThreshold = passingThreshold;
        ReviewMode = reviewMode;
    }

    public override bool IsPublishReady() => CurrentVersionId.HasValue && QuestionsCount > 0;
}
