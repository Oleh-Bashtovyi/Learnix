namespace Learnix.Domain.Common;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-ARCH-008: IDomainEvent without dependency on MediatR — adapter in Application
/// </remarks>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
