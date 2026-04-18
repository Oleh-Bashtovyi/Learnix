using FluentResults;

namespace Learnix.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<(Guid UserId, string EmailConfirmationToken)>> RegisterAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken ct = default);

    Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct = default);

    /// <summary>
    /// Returns Result.Ok if email exists and was sent, or Result.Ok if email doesn't exist (anti-enumeration). 
    /// Result.Fail only on infrastructure errors.
    /// </summary>
    Task<Result> ResendConfirmationEmailAsync(string email, CancellationToken ct = default);
}