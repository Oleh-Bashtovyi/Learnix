using FluentResults;

namespace Learnix.Application.Common.Errors;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-ARCH-004: Typed errors (FluentResults custom errors) instead of string matching
/// </remarks>
public class ConflictError : Error
{
    public ConflictError(string message) : base(message) { }
}
