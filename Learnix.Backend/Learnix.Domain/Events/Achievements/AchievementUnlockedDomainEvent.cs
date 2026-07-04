using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Achievements;

public sealed record AchievementUnlockedDomainEvent(
    Guid UserAchievementId,
    Guid UserId,
    string Code,
    DateTime UnlockedAt
) : DomainEvent;
