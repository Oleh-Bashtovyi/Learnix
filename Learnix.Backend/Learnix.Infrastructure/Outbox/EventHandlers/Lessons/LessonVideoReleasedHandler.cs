using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Lessons;
using MediatR;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Lessons;

internal sealed class LessonVideoReleasedHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<LessonVideoReleasedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<LessonVideoReleasedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;

        holder.DbContext!.OutboxMessages.Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.DeleteBlob,
            Payload = JsonSerializer.Serialize(new DeleteBlobPayload(e.ReleasedBlobPath)),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });

        return Task.CompletedTask;
    }
}
