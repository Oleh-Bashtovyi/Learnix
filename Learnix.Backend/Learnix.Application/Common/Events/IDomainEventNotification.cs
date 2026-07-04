using Learnix.Domain.Common;
using MediatR;

namespace Learnix.Application.Common.Events;

/// <summary>
/// Adapter that wraps a domain event so it can be published via MediatR.
/// Keeps Learnix.Domain free of MediatR dependency.
/// </summary>
public interface IDomainEventNotification<out TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    TDomainEvent DomainEvent { get; }
}

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-ARCH-008: IDomainEvent without dependency on MediatR — adapter in Application
/// </remarks>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent)
    : IDomainEventNotification<TDomainEvent>
    where TDomainEvent : IDomainEvent;
