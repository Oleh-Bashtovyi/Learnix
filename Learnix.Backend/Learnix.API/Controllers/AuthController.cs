using Learnix.API.Extensions;
using Learnix.API.RateLimiting;
using Learnix.Application.Auth.Commands.ConfirmEmail;
using Learnix.Application.Auth.Commands.ForgotPassword;
using Learnix.Application.Auth.Commands.GoogleLogin;
using Learnix.Application.Auth.Commands.Login;
using Learnix.Application.Auth.Commands.Logout;
using Learnix.Application.Auth.Commands.RefreshToken;
using Learnix.Application.Auth.Commands.Register;
using Learnix.Application.Auth.Commands.ResendConfirmationEmail;
using Learnix.Application.Auth.Commands.ResetPassword;
using Learnix.Application.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Learnix.API.Controllers;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-001: JWT (short-lived) + Refresh Token
/// - ADR-BACK-AUTH-007: Refresh token rotation with replay-attack protection
/// - ADR-BACK-AUTH-010: Google OAuth via Google Identity Services (ID token)
/// - ADR-BACK-AUTH-012: Rate limiting — in-memory FixedWindow per IP
/// - ADR-BACK-AUTH-014: Email confirmation soft restriction (Authorize policy)
/// </remarks>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender, IHostEnvironment environment) : ControllerBase
{
    private const string RefreshCookieName = "learnix_refresh";
    private const string RefreshCookiePath = "/api/auth";

    // Registration
    // =================================

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var language = ParseAcceptLanguage(Request.Headers.AcceptLanguage.ToString());
        var result = await sender.Send(command with { Language = language }, ct);

        return result.ToActionResult(onSuccess: response =>
        {
            SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

            return CreatedAtAction(nameof(Register), new
            {
                response.AccessToken,
                response.AccessTokenExpiresAt,
                response.AvatarUrl
            });
        });
    }

    private static string ParseAcceptLanguage(string header) =>
        header.StartsWith("uk", StringComparison.OrdinalIgnoreCase) ? "uk" : "en";

    [HttpPost("confirm-email")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.ToActionResult(onSuccess: response =>
        {
            SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

            return Ok(new
            {
                response.AccessToken,
                response.AccessTokenExpiresAt,
                response.AvatarUrl
            });
        });
    }

    [HttpPost("resend-confirmation")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationEmailCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        // Manual check to preserve anti-enumeration
        if (result.HasError<ValidationError>(out var validationErrors))
        {
            return BadRequest(new ValidationProblemDetails(validationErrors.First().ToDictionary()));
        }

        // Always 204
        return NoContent();
    }

    // Password reset
    // =================================
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ChangePassword([FromBody] Learnix.Application.Auth.Commands.ChangePassword.ChangePasswordCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("set-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> SetPassword([FromBody] Learnix.Application.Auth.Commands.SetPassword.SetPasswordCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult();
    }

    // Login / Refresh / Logout
    // =================================

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.ToActionResult(onSuccess: response =>
        {
            SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

            return Ok(new
            {
                response.AccessToken,
                response.AccessTokenExpiresAt,
                response.AvatarUrl
            });
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "No refresh token."
            });
        }

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), ct);

        // Manual check to preserve the side-effect of clearing the cookie
        // Using AuthenticationError since we migrated from ForbiddenError in previous steps
        if (result.HasError<AuthenticationError>())
        {
            ClearRefreshTokenCookie();
        }

        return result.ToActionResult(onSuccess: response =>
        {
            SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

            return Ok(new
            {
                response.AccessToken,
                response.AccessTokenExpiresAt,
                response.AvatarUrl
            });
        });
    }

    [HttpPost("google")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.ToActionResult(onSuccess: value =>
        {
            SetRefreshTokenCookie(value.RefreshToken, value.RefreshTokenExpiresAt);
            return Ok(new
            {
                value.AccessToken,
                value.AccessTokenExpiresAt,
                value.AvatarUrl
            });
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken);
        var result = await sender.Send(new LogoutCommand(refreshToken ?? string.Empty), ct);
        ClearRefreshTokenCookie();

        return result.ToActionResult();
    }

    // Cookie helpers
    // =================================
    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            // In development we allow plain HTTP on localhost (browsers permit it).
            // Outside development the flag is always true regardless of how TLS is
            // terminated — this is intentionally not derived from Request.IsHttps so
            // that a misconfigured proxy cannot accidentally produce an insecure cookie.
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = expiresAt
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            Path = RefreshCookiePath
        });
    }
}
