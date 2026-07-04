using Learnix.Domain.Common;

namespace Learnix.Domain.Events;

public sealed record PasswordResetRequestedDomainEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string Token) : DomainEvent;
