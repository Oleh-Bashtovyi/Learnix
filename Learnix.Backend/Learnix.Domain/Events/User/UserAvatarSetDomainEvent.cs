using Learnix.Domain.Common;

namespace Learnix.Domain.Events.User;

public sealed record UserAvatarSetDomainEvent(
    Guid UserId,
    string BlobPath
) : DomainEvent;
