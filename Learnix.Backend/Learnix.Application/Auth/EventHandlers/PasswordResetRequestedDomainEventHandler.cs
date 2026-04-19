using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Application.Common.Events;
using Learnix.Application.Common.Settings;
using Learnix.Domain.Events;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Text;

namespace Learnix.Application.Auth.EventHandlers;

internal sealed class PasswordResetRequestedDomainEventHandler(
    IEmailSender emailSender,
    IOptions<AppSettings> appSettings)
    : INotificationHandler<DomainEventNotification<PasswordResetRequestedDomainEvent>>
{
    private readonly AppSettings _appSettings = appSettings.Value;

    public async Task Handle(
        DomainEventNotification<PasswordResetRequestedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        // Identity tokens contain '+', '/', '=' — must be base64-url encoded for safe URL transport
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(domainEvent.Token));
        var encodedEmail = Uri.EscapeDataString(domainEvent.Email);

        var link = $"{_appSettings.ClientBaseUrl}/reset-password" +
                   $"?email={encodedEmail}&token={encodedToken}";

        await emailSender.SendPasswordResetAsync(
            domainEvent.Email, domainEvent.FirstName, link, cancellationToken);
    }
}