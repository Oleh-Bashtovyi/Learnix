using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox.Payloads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Users;

internal sealed class UserUnbannedHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<UserUnbannedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserUnbannedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        var user = await db.Set<User>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == e.UserId)
            .Select(u => new { u.Email, u.FirstName })
            .FirstOrDefaultAsync(ct);

        if (user is null) return;

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.UserUnbannedEmail,
            Payload = JsonSerializer.Serialize(new SendUserUnbannedEmailPayload(user.Email!, user.FirstName)),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });
    }
}
