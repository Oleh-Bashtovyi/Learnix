using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Notifications.Abstractions;
using MediatR;

namespace Learnix.Application.Notifications.Commands.MarkNotificationsReadByType;

internal sealed class MarkNotificationsReadByTypeCommandHandler(
    ICurrentUserService currentUser,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkNotificationsReadByTypeCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationsReadByTypeCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        // Let's implement the repository method to mark all unread of a specific type
        await notificationRepository.MarkAllReadByTypeAsync(currentUser.UserId.Value, request.Type, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
