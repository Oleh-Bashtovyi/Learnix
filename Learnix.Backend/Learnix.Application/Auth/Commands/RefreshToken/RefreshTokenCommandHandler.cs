using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.Login;
using Learnix.Application.Auth.Specifications;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using MediatR;
using Microsoft.Extensions.Logging;
using RefreshTokenEntity = Learnix.Domain.Entities.RefreshToken;

namespace Learnix.Application.Auth.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IUserAuthenticationService authService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashRefreshToken(request.RefreshToken);

        var presented = await refreshTokenRepository.FirstOrDefaultAsync(
            new RefreshTokenByHashSpecification(hash), cancellationToken);

        if (presented is null)
            return Result.Fail<LoginResponse>(new AuthenticationError("Invalid refresh token."));

        // Replay attack: revoked token presented again → burn all active sessions for this user.
        if (presented.IsRevoked)
        {
            logger.LogWarning(
                "Refresh token replay detected for user {UserId}. Revoking all active tokens.",
                presented.UserId);

            var active = await refreshTokenRepository.ListAsync(
                new ActiveRefreshTokensByUserSpecification(presented.UserId), cancellationToken);

            foreach (var t in active)
                t.Revoke();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Fail<LoginResponse>(
                new AuthenticationError("Refresh token reuse detected. All sessions terminated."));
        }

        if (presented.ExpiresAt <= DateTime.UtcNow)
            return Result.Fail<LoginResponse>(new AuthenticationError("Refresh token expired."));

        var userInfoResult = await authService.GetAuthenticationInfoAsync(presented.UserId, cancellationToken);

        if (userInfoResult.IsFailed)
            return Result.Fail<LoginResponse>(userInfoResult.Errors);

        var user = userInfoResult.Value;

        // Rotate: revoke old, issue new pair.
        presented.Revoke();

        var access = tokenService.GenerateAccessToken(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles);

        var newRefresh = tokenService.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(new RefreshTokenEntity(
                user.UserId,
                newRefresh.TokenHash,
                newRefresh.ExpiresAt), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new LoginResponse(
            access.Token,
            access.ExpiresAt,
            newRefresh.PlainToken,
            newRefresh.ExpiresAt));
    }
}
