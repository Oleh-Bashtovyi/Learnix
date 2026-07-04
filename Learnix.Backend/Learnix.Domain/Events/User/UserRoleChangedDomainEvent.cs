using Learnix.Domain.Common;

namespace Learnix.Domain.Events.User;

/// <param name="Assigned">true = role was added; false = role was removed.</param>
public sealed record UserRoleChangedDomainEvent(Guid UserId, string Role, bool Assigned) : DomainEvent;
