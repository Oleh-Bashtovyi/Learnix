using Learnix.Domain.Common;

namespace Learnix.Domain.Events.User;

public sealed record UserBannedDomainEvent(Guid UserId) : DomainEvent;
