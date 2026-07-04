namespace Learnix.Infrastructure.Outbox.Payloads.Achievements;

public sealed record NotifyAchievementUnlockedPayload(
    Guid UserAchievementId,
    Guid UserId,
    string Code,
    DateTime UnlockedAt);
