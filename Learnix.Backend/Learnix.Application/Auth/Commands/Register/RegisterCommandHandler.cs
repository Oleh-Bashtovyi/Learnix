using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.Login;
using Learnix.Application.Common.Abstractions.Persistence;
using MediatR;
using RefreshTokenEntity = Learnix.Domain.Entities.RefreshToken;

namespace Learnix.Application.Auth.Commands.Register;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-006: Decomposition of Identity service
/// - ADR-BACK-AUTH-014: Email confirmation soft restriction
/// </remarks>
internal sealed class RegisterCommandHandler(
    IUserRegistrationService registrationService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var result = await registrationService.RegisterAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Language,
            cancellationToken);

        if (result.IsFailed)
            return Result.Fail<LoginResponse>(result.Errors);

        var userId = result.Value.UserId;

        var access = tokenService.GenerateAccessToken(
            userId,
            request.Email,
            request.FirstName,
            request.LastName,
            new[] { Learnix.Domain.Constants.Roles.Student },
            false);

        var refresh = tokenService.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(
            new RefreshTokenEntity(
                userId,
                refresh.TokenHash,
                refresh.ExpiresAt), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new LoginResponse(
            access.Token, access.ExpiresAt,
            refresh.PlainToken, refresh.ExpiresAt,
            null));
    }
}
