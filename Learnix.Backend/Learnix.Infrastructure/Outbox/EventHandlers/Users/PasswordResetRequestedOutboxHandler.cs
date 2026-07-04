using System.Text;
using Learnix.Application.Common.Events;
using Learnix.Application.Common.Settings;
using Learnix.Domain.Entities;
using Learnix.Domain.Events;
using Learnix.Infrastructure.Outbox.Payloads.Users;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Users;

internal sealed class PasswordResetRequestedOutboxHandler(
    OutboxDbContextHolder holder,
    IOptions<AppSettings> appSettings)
    : INotificationHandler<DomainEventNotification<PasswordResetRequestedDomainEvent>>
{
    private readonly AppSettings _appSettings = appSettings.Value;

    public async Task Handle(
        DomainEventNotification<PasswordResetRequestedDomainEvent> notification,
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

        // Identity tokens contain '+', '/', '=' — must be base64-url encoded for safe URL transport
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(e.Token));
        var encodedEmail = Uri.EscapeDataString(e.Email);

        var link = $"{_appSettings.ClientBaseUrl}/reset-password" +
                   $"?email={encodedEmail}&token={encodedToken}";

        db.OutboxMessages.Add(OutboxMessage.Create(
            e.EventId,
            OutboxMessageTypes.PasswordResetEmail,
            new SendPasswordResetEmailPayload(e.Email, e.FirstName, link, language)));
    }
}
