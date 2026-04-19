using FluentResults;

namespace Learnix.Application.Auth.Abstractions;

public interface IPasswordResetService
{
    /// <summary>
    /// Always returns Result.Ok — anti-enumeration.
    /// Raises PasswordResetRequestedDomainEvent only if user exists and has a confirmed email.
    /// </summary>
    Task<Result> InitiateResetAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Resets the user's password using a token previously issued by InitiateResetAsync.
    /// Returns NotFoundError if user doesn't exist, or generic errors for invalid/expired token
    /// and password policy violations (mapped from Identity).
    /// </summary>
    Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken ct = default);
}