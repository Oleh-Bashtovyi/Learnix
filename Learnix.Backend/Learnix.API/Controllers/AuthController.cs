using Learnix.API.Extensions;
using Learnix.Application.Auth.Commands.ConfirmEmail;
using Learnix.Application.Auth.Commands.ForgotPassword;
using Learnix.Application.Auth.Commands.Login;
using Learnix.Application.Auth.Commands.Logout;
using Learnix.Application.Auth.Commands.RefreshToken;
using Learnix.Application.Auth.Commands.Register;
using Learnix.Application.Auth.Commands.ResendConfirmationEmail;
using Learnix.Application.Auth.Commands.ResetPassword;
using Learnix.Application.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    private const string RefreshCookieName = "learnix_refresh";
    private const string RefreshCookiePath = "/api/auth";

    // Registration
    // =================================

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.ToActionResult(
            onSuccess: value => CreatedAtAction(nameof(Register), value));
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.ToActionResult();
    }

    [HttpPost("resend-confirmation")]
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
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult();
    }

    // Login / Refresh / Logout
    // =================================

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.ToActionResult(response =>
        {
            SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

            return Ok(new
            {
                response.AccessToken,
                response.AccessTokenExpiresAt
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

        return result.ToActionResult(response =>
        {
            SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

            return Ok(new
            {
                response.AccessToken,
                response.AccessTokenExpiresAt
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
            Secure = Request.IsHttps,       // localhost over HTTPS works; plain HTTP localhost is also accepted by browsers
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