using Learnix.Domain.Common;

namespace Learnix.Domain.Events.User;

public sealed record UserUnbannedDomainEvent(Guid UserId) : DomainEvent;
