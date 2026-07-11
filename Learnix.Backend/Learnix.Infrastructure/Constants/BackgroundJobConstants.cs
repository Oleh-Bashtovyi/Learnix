namespace Learnix.Infrastructure.Constants;

public static class BackgroundJobConstants
{
    // Refresh Token Cleanup
    public static readonly TimeSpan RefreshTokenCleanupInterval = TimeSpan.FromHours(24);
    public static readonly TimeSpan RefreshTokenRetentionAfterExpiry = TimeSpan.FromDays(7);

    // Deleted Account Purge — the window itself is UserConstants.AccountRecoveryWindowDays, written onto
    // each user as PurgeAfter. This is only how often we come looking.
    public static readonly TimeSpan DeletedAccountPurgeInterval = TimeSpan.FromHours(24);

    // Maintenance Jobs
    public static readonly TimeSpan CategoryReconciliationInterval = TimeSpan.FromHours(1);
    public static readonly TimeSpan CourseRatingReconciliationInterval = TimeSpan.FromHours(24);
}
