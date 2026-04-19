using FluentResults;
using Learnix.Application.Auth.Abstractions;
using MediatR;

namespace Learnix.Application.Auth.Commands.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(IPasswordResetService passwordResetService)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    public Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        => passwordResetService.InitiateResetAsync(request.Email, cancellationToken);
}
