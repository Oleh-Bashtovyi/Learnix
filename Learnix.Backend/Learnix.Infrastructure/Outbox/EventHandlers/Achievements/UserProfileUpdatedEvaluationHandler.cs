using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

internal sealed class UserProfileUpdatedEvaluationHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<UserProfileUpdatedDomainEvent, EvaluateProfileChangedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.EvaluateProfileChanged;
    protected override EvaluateProfileChangedPayload BuildPayload(UserProfileUpdatedDomainEvent e)
        => new(e.UserId);
}
