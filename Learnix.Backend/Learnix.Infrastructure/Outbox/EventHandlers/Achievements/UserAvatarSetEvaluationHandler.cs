using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

/// <summary>
/// Avatar changes feed into the same profile-completeness check as <see cref="UserProfileUpdatedDomainEvent"/>.
/// </summary>
internal sealed class UserAvatarSetEvaluationHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<UserAvatarSetDomainEvent, EvaluateProfileChangedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.EvaluateProfileChanged;
    protected override EvaluateProfileChangedPayload BuildPayload(UserAvatarSetDomainEvent e)
        => new(e.UserId);
}
