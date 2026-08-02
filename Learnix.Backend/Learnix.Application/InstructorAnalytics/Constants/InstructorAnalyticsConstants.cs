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

    /// <summary>
    /// Window each summary trend measures, and the length of the earlier window it is compared
    /// against. Fixed rather than a request parameter: the figure it sits next to is an all-time
    /// total, so the window is part of what the tile means, not something the caller chooses.
    /// </summary>
    public const int SummaryTrendWindowDays = 30;
}
