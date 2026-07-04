using Learnix.Domain.Common;

namespace Learnix.Domain.Events.User;

public sealed record UserAvatarRemovedDomainEvent(
    Guid UserId,
    string ReleasedBlobPath
) : DomainEvent;
