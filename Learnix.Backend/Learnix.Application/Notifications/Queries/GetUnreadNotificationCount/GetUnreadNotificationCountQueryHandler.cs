using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Application.Notifications.Specifications;
using MediatR;

namespace Learnix.Application.Notifications.Queries.GetUnreadNotificationCount;

internal sealed class GetUnreadNotificationCountQueryHandler(
    ICurrentUserService currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<GetUnreadNotificationCountQuery, Result<UnreadNotificationCountResponse>>
{
    public async Task<Result<UnreadNotificationCountResponse>> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var count = await notificationRepository.CountAsync(
            new UnreadNotificationsByUserSpecification(currentUser.UserId.Value),
            cancellationToken);

        return Result.Ok(new UnreadNotificationCountResponse(count));
    }
}
