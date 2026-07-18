namespace Learnix.Application.InstructorAnalytics.Constants;

public static class InstructorAnalyticsConstants
{
    public const int MinRecentReviewsTake = 1;
    public const int MaxRecentReviewsTake = 100;

    /// <summary>
    /// Upper bound on the <c>dynamics</c> date range. The handler materialises one bucket per day, so an
    /// unbounded (or accidental <c>default(DateTime)</c>) range would build a runaway list — this caps it.
    /// </summary>
    public const int MaxDynamicsRangeDays = 366;

    /// <summary>Window for the "active students" engagement metric.</summary>
    public const int ActiveStudentWindowDays = 30;
}
