using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Application.Common.Events;
using Learnix.Application.Common.Settings;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Events;
using MediatR;
using Microsoft.Extensions.Options;

namespace Learnix.Application.Auth.EventHandlers;

internal sealed class UserRegisteredDomainEventHandler(
    IEmailSender emailSender,
    IUserRepository userRepository,
    IOptions<AppSettings> appSettings)
    : INotificationHandler<DomainEventNotification<UserRegisteredDomainEvent>>
{
    private readonly AppSettings _appSettings = appSettings.Value;

    public async Task Handle(DomainEventNotification<UserRegisteredDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var user = await userRepository.FirstOrDefaultAsync(
            new UserByIdSpecification(domainEvent.UserId), cancellationToken);

        var language = user?.Language ?? "en";

        // Identity TOTP 6-digit code does not need base64 encoding
        var confirmationCode = domainEvent.EmailConfirmationToken;

        await emailSender.SendEmailConfirmationAsync(domainEvent.Email, domainEvent.FirstName, confirmationCode, language, cancellationToken);
    }
}
