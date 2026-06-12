using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.InstructorApplications;
using Learnix.Infrastructure.Outbox.Payloads;
using Learnix.Infrastructure.Outbox.Payloads.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
            .Select(u => new { u.Email, u.FirstName, u.Language })
            .FirstOrDefaultAsync(ct);

        if (user is null) return;

        db.OutboxMessages.Add(OutboxMessage.Create(
            e.EventId,
            OutboxMessageTypes.InstructorApprovedEmail,
            new SendInstructorApprovedEmailPayload(user.Email!, user.FirstName, user.Language)));

        db.OutboxMessages.Add(OutboxMessage.Create(
            Guid.NewGuid(),
            OutboxMessageTypes.NotifyInstructorApproved,
            new NotifyInstructorApprovedPayload(e.UserId)));
    }
}
