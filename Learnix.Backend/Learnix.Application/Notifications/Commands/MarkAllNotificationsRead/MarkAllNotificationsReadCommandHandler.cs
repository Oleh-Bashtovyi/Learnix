using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Notifications.Abstractions;
using MediatR;

namespace Learnix.Application.Notifications.Commands.MarkAllNotificationsRead;

internal sealed class MarkAllNotificationsReadCommandHandler(
    ICurrentUserService currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        await notificationRepository.MarkAllReadAsync(currentUser.UserId.Value, cancellationToken);

        return Result.Ok();
    }
}
