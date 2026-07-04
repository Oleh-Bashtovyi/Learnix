using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.Auth.Commands.SetPassword;

internal sealed class SetPasswordCommandHandler(
    ISetPasswordService setPasswordService,
    ICurrentUserService currentUserService)
    : IRequestHandler<SetPasswordCommand, Result>
{
    public Task<Result> Handle(SetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Task.FromResult(Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated)));

        return setPasswordService.SetPasswordAsync(
            currentUserService.UserId.Value,
            request.NewPassword,
            cancellationToken);
    }
}
