using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox.Payloads.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Users;

internal sealed class UserRoleChangedHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<UserRoleChangedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserRoleChangedDomainEvent> notification,
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
            OutboxMessageTypes.UserRoleChangedEmail,
            new SendUserRoleChangedEmailPayload(user.Email!, user.FirstName, e.Role, e.Assigned, user.Language)));
    }
}
