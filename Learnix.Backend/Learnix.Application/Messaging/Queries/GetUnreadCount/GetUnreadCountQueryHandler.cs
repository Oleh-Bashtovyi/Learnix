using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Messaging.Queries.GetUnreadCount;

public sealed class GetUnreadCountQueryHandler(
    ICurrentUserService currentUser,
    IConversationRepository conversationRepository)
    : IRequestHandler<GetUnreadCountQuery, Result<UnreadCountDto>>
{
    public async Task<Result<UnreadCountDto>> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;
        var isInstructor = currentUser.IsInRole(Roles.Instructor) || currentUser.IsInRole(Roles.Admin);

        var count = await conversationRepository.GetTotalUnreadAsync(userId, isInstructor, cancellationToken);

        return Result.Ok(new UnreadCountDto(count));
    }
}
