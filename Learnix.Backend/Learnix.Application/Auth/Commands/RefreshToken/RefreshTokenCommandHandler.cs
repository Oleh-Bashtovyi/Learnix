using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.Login;
using Learnix.Application.Auth.Constants;
using Learnix.Application.Auth.Specifications;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using MediatR;
using Microsoft.Extensions.Logging;
using RefreshTokenEntity = Learnix.Domain.Entities.RefreshToken;

namespace Learnix.Application.Auth.Commands.RefreshToken;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-007: Refresh token rotation with replay-attack protection
/// - ADR-BACK-AUTH-009: Separation of AuthenticationError and ForbiddenError
/// </remarks>
internal sealed class RefreshTokenCommandHandler(
    IUserAuthenticationService authService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IBlobStorageService blobStorage,
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
            return Result.Fail<LoginResponse>(new AuthenticationError(AuthMessages.InvalidRefreshToken));

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
            return Result.Fail<LoginResponse>(new AuthenticationError(AuthMessages.RefreshTokenExpired));

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
            user.Roles,
            user.EmailConfirmed);

        var newRefresh = tokenService.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(new RefreshTokenEntity(
                user.UserId,
                newRefresh.TokenHash,
                newRefresh.ExpiresAt), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var avatarUrl = !string.IsNullOrWhiteSpace(user.AvatarBlobPath) ? blobStorage.GetPublicUrl(user.AvatarBlobPath) : null;

        return Result.Ok(new LoginResponse(
            access.Token,
            access.ExpiresAt,
            newRefresh.PlainToken,
            newRefresh.ExpiresAt,
            avatarUrl));
    }
}
