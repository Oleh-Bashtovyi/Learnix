using FluentResults;
using Learnix.Application.Auth.Models;

namespace Learnix.Application.Auth.Abstractions;

public interface IUserAuthenticationService
{
    /// <summary>Validates email+password, handles lockout and unconfirmed-email cases.</summary>
    Task<Result<UserAuthenticationInfo>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken ct = default);

    /// <summary>Loads user + roles for token regeneration during refresh.</summary>
    Task<Result<UserAuthenticationInfo>> GetAuthenticationInfoAsync(
        Guid userId,
        CancellationToken ct = default);
}
