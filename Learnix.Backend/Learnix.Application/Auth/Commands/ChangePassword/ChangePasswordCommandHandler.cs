using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.Auth.Commands.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    IChangePasswordService changePasswordService,
    ICurrentUserService currentUserService)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Task.FromResult(Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated)));

        return changePasswordService.ChangePasswordAsync(
            currentUserService.UserId.Value,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);
    }
}
