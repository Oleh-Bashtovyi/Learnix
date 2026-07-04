using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Users;

internal sealed class UserRegisteredOutboxHandler(
    OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<UserRegisteredDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserRegisteredDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        var user = await db.Set<User>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == e.UserId)
            .Select(u => new { u.Language })
            .FirstOrDefaultAsync(ct);

        var language = user?.Language ?? "en";
        var confirmationCode = e.EmailConfirmationToken;

        db.OutboxMessages.Add(OutboxMessage.Create(
            e.EventId,
            OutboxMessageTypes.EmailConfirmation,
            new SendEmailConfirmationPayload(e.Email, e.FirstName, confirmationCode, language)));
    }
}
