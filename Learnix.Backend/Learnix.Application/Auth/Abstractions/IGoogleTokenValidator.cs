using FluentResults;
using Learnix.Application.Auth.Models;

namespace Learnix.Application.Auth.Abstractions;

public interface IGoogleTokenValidator
{
    /// <summary>
    /// Validates the Google ID token (signature, issuer, audience, expiry).
    /// Returns AuthenticationError if token is invalid/expired/untrusted.
    /// </summary>
    Task<Result<GoogleUserInfo>> ValidateAsync(string idToken, CancellationToken ct = default);
}
