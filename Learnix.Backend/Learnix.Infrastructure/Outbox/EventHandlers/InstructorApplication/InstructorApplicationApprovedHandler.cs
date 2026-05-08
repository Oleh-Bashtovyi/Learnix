using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.InstructorApplications;
using Learnix.Infrastructure.Outbox.Payloads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers.InstructorApplication;

internal sealed class InstructorApplicationApprovedHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<InstructorApplicationApprovedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<InstructorApplicationApprovedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        var user = await db.Set<User>()
            .AsNoTracking()
            .Where(u => u.Id == e.UserId)
            .Select(u => new { u.Email, u.FirstName })
            .FirstOrDefaultAsync(ct);

        if (user is null) return;

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.InstructorApprovedEmail,
            Payload = JsonSerializer.Serialize(new SendInstructorApprovedEmailPayload(user.Email!, user.FirstName)),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });
    }
}
