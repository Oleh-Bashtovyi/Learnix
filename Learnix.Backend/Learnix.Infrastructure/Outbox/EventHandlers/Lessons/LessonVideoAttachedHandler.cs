using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Lessons;
using MediatR;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Lessons;

internal sealed class LessonVideoAttachedHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<LessonVideoAttachedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<LessonVideoAttachedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;

        holder.DbContext!.OutboxMessages.Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.MarkBlobConfirmed,
            Payload = JsonSerializer.Serialize(new MarkBlobConfirmedPayload(e.AttachedBlobPath)),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });

        return Task.CompletedTask;
    }
}
