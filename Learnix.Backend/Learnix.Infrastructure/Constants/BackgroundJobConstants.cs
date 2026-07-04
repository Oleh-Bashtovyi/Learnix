namespace Learnix.Infrastructure.Constants;

public static class BackgroundJobConstants
{
    // Chat Session Cleanup
    public static readonly TimeSpan ChatSessionCleanupInterval = TimeSpan.FromHours(1);
    public static readonly TimeSpan ChatSessionRetentionPeriod = TimeSpan.FromDays(30);

    // Refresh Token Cleanup
    public static readonly TimeSpan RefreshTokenCleanupInterval = TimeSpan.FromHours(24);
    public static readonly TimeSpan RefreshTokenRetentionAfterExpiry = TimeSpan.FromDays(7);

    // Maintenance Jobs
    public static readonly TimeSpan CategoryReconciliationInterval = TimeSpan.FromHours(1);
    public static readonly TimeSpan CourseRatingReconciliationInterval = TimeSpan.FromHours(24);
}
