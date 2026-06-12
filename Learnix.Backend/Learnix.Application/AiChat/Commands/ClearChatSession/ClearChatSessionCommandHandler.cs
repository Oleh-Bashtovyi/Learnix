using FluentResults;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.AiChat.Commands.ClearChatSession;

internal sealed class ClearChatSessionCommandHandler(
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<ClearChatSessionCommand, Result>
{
    public async Task<Result> Handle(ClearChatSessionCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("User is not authenticated."));

        var session = await sessionRepository.GetActiveByUserIdAsync(currentUser.UserId.Value, cancellationToken);

        if (session is not null)
            await sessionRepository.CloseSessionAsync(session.Id, cancellationToken);

        return Result.Ok();
    }
}
