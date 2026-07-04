using FluentResults;

namespace Learnix.Application.Common.Errors;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-009: Separation of AuthenticationError and ForbiddenError
/// </remarks>
public sealed class AuthenticationError : Error
{
    public AuthenticationError(string message) : base(message) { }
}
