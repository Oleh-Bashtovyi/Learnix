namespace Learnix.Application.Reviews.Constants;

/// <summary>
/// Application-layer policies for reviewing (ADR-BACK-ARCH-018: not domain invariants — a review with
/// zero completed lessons is still a valid entity; this is a gate the use case enforces, not the type).
/// </summary>
public static class ReviewPolicy
{
    /// <summary>
    /// A student must finish at least this many lessons before they can review a course — a light
    /// anti-abuse gate that stops fresh enrollments (bots, spam) from rating a course they never opened.
    /// </summary>
    public const int MinCompletedLessonsToReview = 1;
}
