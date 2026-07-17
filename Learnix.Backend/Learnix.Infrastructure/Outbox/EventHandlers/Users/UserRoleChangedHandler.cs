using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox.Payloads.Notifications;
using Learnix.Infrastructure.Outbox.Payloads.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Users;

internal sealed class UserRoleChangedHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<UserRoleChangedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserRoleChangedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        var user = await db.Set<User>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == e.UserId)
            .Select(u => new { u.Email, u.FirstName, u.Language })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null) return;

        db.OutboxMessages.Add(OutboxMessage.Create(
            e.EventId,
            OutboxMessageTypes.UserRoleChangedEmail,
            new SendUserRoleChangedEmailPayload(user.Email!, user.FirstName, e.Role, e.Assigned, user.Language)));

        // The email and the bell are not redundant: the email reaches someone who is away, the bell reaches
        // someone who is here and can act on it now — and only the bell can hand them a link into the part of
        // the app the role just opened. An email that lands in spam is also the whole reason the bell exists.
        db.OutboxMessages.Add(OutboxMessage.Create(
            Guid.NewGuid(),
            OutboxMessageTypes.NotifyRoleChanged,
            new NotifyRoleChangedPayload(e.UserId, e.Role, e.Assigned)));
    }
}
