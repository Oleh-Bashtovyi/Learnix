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
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var language = ParseAcceptLanguage(Request.Headers.AcceptLanguage.ToString());
        var result = await sender.Send(command with { Language = language }, cancellationToken);

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
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

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
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

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
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ChangePassword([FromBody] Learnix.Application.Auth.Commands.ChangePassword.ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("set-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> SetPassword([FromBody] Learnix.Application.Auth.Commands.SetPassword.SetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    // Login / Refresh / Logout
    // =================================

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

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
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "No refresh token."
            });
        }

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);

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
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

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
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken);
        var result = await sender.Send(new LogoutCommand(refreshToken ?? string.Empty), cancellationToken);
        ClearRefreshTokenCookie();

        return result.ToActionResult();
    }

    // Cookie helpers
    // =================================
    /// <summary>
    /// The attributes of the refresh cookie. Deleting a cookie is just re-issuing it with an expiry in
    /// the past, so the delete must carry the same attributes as the original — a cross-site logout
    /// response whose Set-Cookie lacks SameSite=None and Secure is dropped by the browser, leaving the
    /// cookie alive until it expires on its own.
    /// </summary>
    // S2092: the Secure flag is a constant true outside Development, where the only origin is
    // http://localhost. It is deliberately not derived from Request.IsHttps, so that a misconfigured
    // proxy in front of the API cannot downgrade the cookie.
#pragma warning disable S2092
    private CookieOptions BuildRefreshCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        // In production the frontend and the API sit on different domains (azurestaticapps vs
        // azurecontainerapps), so the browser only accepts the cookie with SameSite=None.
        SameSite = environment.IsDevelopment() ? SameSiteMode.Strict : SameSiteMode.None,
        Path = RefreshCookiePath
    };
#pragma warning restore S2092

    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        var options = BuildRefreshCookieOptions();
        options.Expires = expiresAt;

        Response.Cookies.Append(RefreshCookieName, token, options);
    }

    private void ClearRefreshTokenCookie()
        => Response.Cookies.Delete(RefreshCookieName, BuildRefreshCookieOptions());
}
