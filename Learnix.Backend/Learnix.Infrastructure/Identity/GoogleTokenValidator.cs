using FluentResults;
using Google.Apis.Auth;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Models;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnix.Infrastructure.Identity;

internal sealed class GoogleTokenValidator(
    IOptions<GoogleSettings> googleSettings,
    ILogger<GoogleTokenValidator> logger)
    : IGoogleTokenValidator
{
    private readonly GoogleSettings _settings = googleSettings.Value;

    public async Task<Result<GoogleUserInfo>> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_settings.ClientId]
                    // ValidateAsync internally checks: signature, issuer (accounts.google.com /
                    // https://accounts.google.com), expiry, and matches audience to our ClientId.
                });
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Invalid Google ID token presented.");
            return Result.Fail<GoogleUserInfo>(new AuthenticationError("Invalid Google token."));
        }

        if (!payload.EmailVerified)
        {
            logger.LogWarning("Google token for {Email} has EmailVerified=false — rejecting.", payload.Email);
            return Result.Fail<GoogleUserInfo>(
                new AuthenticationError("Google email is not verified."));
        }

        return Result.Ok(new GoogleUserInfo(
            GoogleId: payload.Subject,
            Email: payload.Email,
            EmailVerified: payload.EmailVerified,
            FirstName: payload.GivenName,
            LastName: payload.FamilyName));
    }
}
