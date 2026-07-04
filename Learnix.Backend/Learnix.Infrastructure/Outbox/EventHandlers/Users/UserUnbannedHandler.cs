using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox.Payloads.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
            .Select(u => new { u.Email, u.FirstName, u.Language })
            .FirstOrDefaultAsync(ct);

        if (user is null) return;

        db.OutboxMessages.Add(OutboxMessage.Create(
            e.EventId,
            OutboxMessageTypes.UserUnbannedEmail,
            new SendUserUnbannedEmailPayload(user.Email!, user.FirstName, user.Language)));
    }
}
