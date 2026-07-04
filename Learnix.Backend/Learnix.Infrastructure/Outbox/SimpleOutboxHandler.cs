using Learnix.Application.Common.Events;
using Learnix.Domain.Common;
using MediatR;

namespace Learnix.Infrastructure.Outbox;

internal abstract class SimpleOutboxHandler<TEvent, TPayload>(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<TEvent>>
    where TEvent : DomainEvent
{
    protected abstract string MessageType { get; }
    protected abstract TPayload BuildPayload(TEvent e);

    public Task Handle(DomainEventNotification<TEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        holder.DbContext!.OutboxMessages.Add(
            OutboxMessage.Create(e.EventId, MessageType, BuildPayload(e)));
        return Task.CompletedTask;
    }
}
