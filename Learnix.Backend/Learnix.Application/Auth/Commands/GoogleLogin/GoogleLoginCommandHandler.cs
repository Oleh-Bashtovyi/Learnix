using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.Login;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using MediatR;
using RefreshTokenEntity = Learnix.Domain.Entities.RefreshToken;

namespace Learnix.Application.Auth.Commands.GoogleLogin;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-010: Google OAuth via Google Identity Services
/// - ADR-BACK-AUTH-011: GoogleId as denormalized field on User
/// - ADR-BACK-AUTH-014: Email confirmation soft restriction
/// </remarks>
internal sealed class GoogleLoginCommandHandler(
    IGoogleTokenValidator googleTokenValidator,
    IUserRegistrationService userRegistrationService,
    IUserAuthenticationService userAuthenticationService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GoogleLoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate the Google ID token (signature, audience, expiry, email_verified)
        var validateResult = await googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);
        if (validateResult.IsFailed)
            return Result.Fail<LoginResponse>(validateResult.Errors);

        var googleUser = validateResult.Value;

        // 2. Find-or-create the local user
        var findOrCreateResult = await userRegistrationService.FindOrCreateGoogleUserAsync(googleUser, cancellationToken);
        if (findOrCreateResult.IsFailed)
            return Result.Fail<LoginResponse>(findOrCreateResult.Errors);

        var userId = findOrCreateResult.Value;

        // 3. Load auth info (email + name + roles) for token generation
        var authInfoResult = await userAuthenticationService.GetAuthenticationInfoAsync(userId, cancellationToken);
        if (authInfoResult.IsFailed)
            return Result.Fail<LoginResponse>(authInfoResult.Errors);

        var authInfo = authInfoResult.Value;

        // 4. Generate tokens (same flow as regular Login)
        var access = tokenService.GenerateAccessToken(
            authInfo.UserId, authInfo.Email, authInfo.FirstName, authInfo.LastName,
            authInfo.Roles, authInfo.EmailConfirmed);
        var refresh = tokenService.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(
            new RefreshTokenEntity(authInfo.UserId, refresh.TokenHash, refresh.ExpiresAt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var avatarUrl = !string.IsNullOrWhiteSpace(authInfo.AvatarBlobPath) ? blobStorage.GetPublicUrl(authInfo.AvatarBlobPath) : null;

        return Result.Ok(new LoginResponse(
            access.Token, access.ExpiresAt,
            refresh.PlainToken, refresh.ExpiresAt,
            avatarUrl));
    }
}
