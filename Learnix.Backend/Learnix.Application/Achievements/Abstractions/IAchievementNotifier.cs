namespace Learnix.Application.Achievements.Abstractions;

public interface IAchievementNotifier
{
    Task NotifyAsync(Guid userId, Guid achievementId, string code, DateTime unlockedAt, CancellationToken ct);
}
