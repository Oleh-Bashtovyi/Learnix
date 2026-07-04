using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Application.Notifications.Constants;
using Learnix.Application.Notifications.Specifications;
using MediatR;

namespace Learnix.Application.Notifications.Commands.MarkNotificationRead;

internal sealed class MarkNotificationReadCommandHandler(
    ICurrentUserService currentUser,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var notification = await notificationRepository.FirstOrDefaultAsync(
            new NotificationByIdAndUserSpecification(request.NotificationId, currentUser.UserId.Value),
            cancellationToken);

        if (notification is null)
            return Result.Fail(new NotFoundError(NotificationMessages.NotificationIdNotFound(request.NotificationId)));

        notification.MarkRead();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
