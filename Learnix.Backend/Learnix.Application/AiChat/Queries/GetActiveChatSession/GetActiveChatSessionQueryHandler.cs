using FluentResults;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetActiveChatSession;

internal sealed class GetActiveChatSessionQueryHandler(
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<GetActiveChatSessionQuery, Result<ChatSessionDto>>
{
    public async Task<Result<ChatSessionDto>> Handle(
        GetActiveChatSessionQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("User is not authenticated."));

        var session = await sessionRepository.GetActiveByUserIdAsync(currentUser.UserId.Value, cancellationToken);

        if (session is null)
            return Result.Ok(new ChatSessionDto(string.Empty, []));

        var messages = session.Messages
            .Where(m => m.Role is "user" or "assistant")
            .Select(m => new ChatMessageDto(m.Role, m.Content, m.SentAt))
            .ToList();

        return Result.Ok(new ChatSessionDto(session.Id, messages));
    }
}
