using FluentResults;
using Learnix.Application.Auth.Abstractions;
using MediatR;

namespace Learnix.Application.Auth.Commands.ResendConfirmationEmail;

internal sealed class ResendConfirmationEmailCommandHandler(IUserRegistrationService registrationService)
    : IRequestHandler<ResendConfirmationEmailCommand, Result>
{
    public Task<Result> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
        => registrationService.ResendConfirmationEmailAsync(request.Email, cancellationToken);
}
