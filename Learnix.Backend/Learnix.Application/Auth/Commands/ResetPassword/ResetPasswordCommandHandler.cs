using FluentResults;
using Learnix.Application.Auth.Abstractions;
using MediatR;

namespace Learnix.Application.Auth.Commands.ResetPassword;

internal sealed class ResetPasswordCommandHandler(IPasswordResetService passwordResetService)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        => passwordResetService.ResetPasswordAsync(
            request.Email, request.Token, request.NewPassword, cancellationToken);
}
