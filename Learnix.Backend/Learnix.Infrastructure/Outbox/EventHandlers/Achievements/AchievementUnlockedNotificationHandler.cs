using Learnix.Domain.Events.Achievements;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

/// <summary>
/// When an achievement is unlocked, enqueue a notification message.
/// Phase 2 will dispatch this to SignalR; for now the message is recorded
/// and the dispatcher (no-op) leaves it processed.
/// </summary>
internal sealed class AchievementUnlockedNotificationHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<AchievementUnlockedDomainEvent, NotifyAchievementUnlockedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.NotifyAchievementUnlocked;
    protected override NotifyAchievementUnlockedPayload BuildPayload(AchievementUnlockedDomainEvent e)
        => new(e.UserAchievementId, e.UserId, e.Code, e.UnlockedAt);
}
