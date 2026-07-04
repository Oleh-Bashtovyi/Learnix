using FluentResults;
using Learnix.Application.Auth.Abstractions;
using MediatR;

namespace Learnix.Application.Auth.Commands.Register;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-006: Decomposition of Identity service
/// - ADR-BACK-AUTH-014: Email confirmation soft restriction
/// </remarks>
internal sealed class RegisterCommandHandler(IUserRegistrationService registrationService)
    : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var result = await registrationService.RegisterAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Language,
            cancellationToken);

        return result.IsFailed
            ? Result.Fail<RegisterResponse>(result.Errors)
            : Result.Ok(new RegisterResponse(result.Value.UserId, request.Email));
    }
}
