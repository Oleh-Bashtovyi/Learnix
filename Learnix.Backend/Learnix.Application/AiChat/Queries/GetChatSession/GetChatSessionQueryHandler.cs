using FluentResults;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Services;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetChatSession;

internal sealed class GetChatSessionQueryHandler(
    IChatSessionRepository sessionRepository,
    ChatScopeAuthorizer authorizer,
    ICurrentUserService currentUser)
    : IRequestHandler<GetChatSessionQuery, Result<ChatSessionDto>>
{
    public async Task<Result<ChatSessionDto>> Handle(
        GetChatSessionQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;

        var access = await authorizer.EnsureAccessAsync(userId, request.Scope, cancellationToken);
        if (access.IsFailed)
            return Result.Fail<ChatSessionDto>(access.Errors);

        var session = await sessionRepository.GetByScopeAsync(userId, request.Scope, cancellationToken);

        if (session is null)
            return Result.Ok(new ChatSessionDto(string.Empty, []));

        // A tool-calling assistant turn carries no text of its own — replaying it would render an empty bubble.
        var messages = session.Messages
            .Where(m => m.Role is ChatMessageRoles.User or ChatMessageRoles.Assistant && !string.IsNullOrEmpty(m.Content))
            .Select(m => new ChatMessageDto(m.Role, m.Content, m.SentAt))
            .ToList();

        return Result.Ok(new ChatSessionDto(session.Id, messages));
    }
}
