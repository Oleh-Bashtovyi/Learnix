using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.Login;
using Learnix.Application.Auth.Constants;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using RefreshTokenEntity = Learnix.Domain.Entities.RefreshToken;

namespace Learnix.Application.Auth.Commands.ConfirmEmail;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-016: 6-Digit OTP for Email Confirmation instead of Magic Link
/// </remarks>
internal sealed class ConfirmEmailCommandHandler(
    UserManager<User> userManager,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmEmailCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result.Fail(new NotFoundError(CommonMessages.UserNotFound));

        if (user.EmailConfirmed)
            return Result.Fail(new ConflictError(AuthMessages.EmailAlreadyConfirmed));

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
            return Result.Fail(new AuthenticationError(AuthMessages.InvalidConfirmationCode));

        var roles = await userManager.GetRolesAsync(user);

        var access = tokenService.GenerateAccessToken(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles.ToList(),
            true); // Hardcoded true because we just successfully confirmed it

        var refresh = tokenService.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(
            new RefreshTokenEntity(
                user.Id,
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
