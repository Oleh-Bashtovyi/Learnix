namespace Learnix.Application.Achievements.Abstractions;

public interface IAchievementsHubClient
{
    Task AchievementUnlocked(AchievementUnlockedNotification notification);
}

public sealed record AchievementUnlockedNotification(
    Guid AchievementId,
    string Code,
    DateTime UnlockedAt);
