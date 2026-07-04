using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Application.Notifications.Specifications;
using MediatR;

namespace Learnix.Application.Notifications.Queries.GetMyNotifications;

internal sealed class GetMyNotificationsQueryHandler(
    ICurrentUserService currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<GetMyNotificationsQuery, Result<IReadOnlyList<NotificationDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var notifications = await notificationRepository.ListAsync(
            new NotificationsByUserSpecification(currentUser.UserId.Value),
            cancellationToken);

        var dtos = notifications
            .Select(n => new NotificationDto(n.Id, n.Type.ToString(), n.Title, n.Body, n.IsRead, n.CreatedAt))
            .ToList();

        return Result.Ok<IReadOnlyList<NotificationDto>>(dtos);
    }
}
