using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using MediatR;
using RefreshTokenEntity = Learnix.Domain.Entities.RefreshToken;

namespace Learnix.Application.Auth.Commands.Login;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-001: JWT (short-lived) + Refresh Token
/// - ADR-BACK-AUTH-006: Decomposition of Identity service
/// - ADR-BACK-AUTH-014: Email confirmation soft restriction
/// </remarks>
internal sealed class LoginCommandHandler(
    IUserAuthenticationService authService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var authResult = await authService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (authResult.IsFailed)
            return Result.Fail<LoginResponse>(authResult.Errors);

        var user = authResult.Value;

        var access = tokenService.GenerateAccessToken(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles,
            user.EmailConfirmed);

        var refresh = tokenService.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(
            new RefreshTokenEntity(
                user.UserId,
                refresh.TokenHash,
                refresh.ExpiresAt), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var avatarUrl = !string.IsNullOrWhiteSpace(user.AvatarBlobPath) ? blobStorage.GetPublicUrl(user.AvatarBlobPath) : null;

        return Result.Ok(new LoginResponse(
            access.Token, access.ExpiresAt,
            refresh.PlainToken, refresh.ExpiresAt,
            avatarUrl));
    }
}
