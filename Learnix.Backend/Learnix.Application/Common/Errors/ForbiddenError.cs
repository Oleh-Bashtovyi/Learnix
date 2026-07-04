using FluentResults;

namespace Learnix.Application.Common.Errors;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-009: Separation of AuthenticationError and ForbiddenError
/// </remarks>
public sealed class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message) { }
}
