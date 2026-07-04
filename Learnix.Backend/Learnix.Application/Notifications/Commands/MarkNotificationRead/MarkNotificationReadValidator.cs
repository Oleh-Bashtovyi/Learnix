using FluentValidation;

namespace Learnix.Application.Notifications.Commands.MarkNotificationRead;

internal sealed class MarkNotificationReadValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
