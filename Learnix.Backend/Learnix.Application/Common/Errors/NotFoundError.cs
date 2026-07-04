using FluentResults;

namespace Learnix.Application.Common.Errors;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-ARCH-004: Typed errors (FluentResults custom errors) instead of string matching
/// </remarks>
public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message) { }
}
