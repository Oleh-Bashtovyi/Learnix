using Learnix.Domain.Events.User;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Users;

internal sealed class UserAvatarRemovedOutboxHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<UserAvatarRemovedDomainEvent, DeleteBlobPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.DeleteBlob;
    protected override DeleteBlobPayload BuildPayload(UserAvatarRemovedDomainEvent e)
        => new(e.ReleasedBlobPath);
}
