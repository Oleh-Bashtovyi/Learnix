using FluentResults;
using Learnix.Application.Auth.Models;

namespace Learnix.Application.Auth.Abstractions;

public interface IUserRegistrationService
{
    Task<Result<(Guid UserId, string EmailConfirmationToken)>> RegisterAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string language = "en",
        CancellationToken ct = default);

    Task<Result> ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken ct = default);

    /// <summary>Always returns Result.Ok — anti-enumeration.</summary>
    Task<Result> ResendConfirmationEmailAsync(
        string email,
        CancellationToken ct = default);

    /// <summary>
    /// Find-or-create for Google OAuth users.
    /// - GoogleId match → return existing user.
    /// - Email match + confirmed → link Google to existing account.
    /// - Email match + NOT confirmed → takeover: wipe password, confirm email, link Google.
    /// - No match → create new user (PasswordHash null, EmailConfirmed true, Student role).
    /// Returns the user's Id in all success cases.
    /// </summary>
    Task<Result<Guid>> FindOrCreateGoogleUserAsync(
        GoogleUserInfo googleUser,
        CancellationToken ct = default);
}
