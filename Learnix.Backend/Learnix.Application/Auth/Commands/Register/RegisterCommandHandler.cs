using FluentResults;
using Learnix.Application.Common.Interfaces;
using MediatR;

namespace Learnix.Application.Auth.Commands.Register;

internal sealed class RegisterCommandHandler(IIdentityService identityService)
    : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.RegisterAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        if (result.IsFailed)
            return Result.Fail<RegisterResponse>(result.Errors);

        // IdentityService raises UserRegisteredDomainEvent → handled by UserRegisteredDomainEventHandler → sends email
        return Result.Ok(new RegisterResponse(result.Value.UserId, request.Email));
    }
}
