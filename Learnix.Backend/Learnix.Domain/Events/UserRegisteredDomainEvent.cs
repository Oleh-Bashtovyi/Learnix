using Learnix.Domain.Common;

namespace Learnix.Domain.Events;

public sealed record UserRegisteredDomainEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string EmailConfirmationToken
) : DomainEvent;
