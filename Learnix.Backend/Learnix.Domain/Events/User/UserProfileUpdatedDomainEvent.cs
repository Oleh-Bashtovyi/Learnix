using Learnix.Domain.Common;

namespace Learnix.Domain.Events.User;

public sealed record UserProfileUpdatedDomainEvent(Guid UserId) : DomainEvent;
