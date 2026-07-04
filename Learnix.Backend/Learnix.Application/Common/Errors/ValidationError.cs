using FluentResults;
using FluentValidation.Results;

namespace Learnix.Application.Common.Errors;

/// <summary>
/// Carries FluentValidation's ValidationResult so consumers can project it
/// into whatever shape they need (ProblemDetails dictionary, logs, etc.).
/// </summary>
/// <remarks>
/// Related ADRs:
/// - ADR-BACK-ARCH-004: Typed errors (FluentResults custom errors) instead of string matching
/// </remarks>
public sealed class ValidationError : Error
{
    public ValidationResult ValidationResult { get; }

    public ValidationError(ValidationResult validationResult)
        : base("One or more validation errors occurred.")
    {
        ValidationResult = validationResult;
    }

    public Dictionary<string, string[]> ToDictionary()
        => ValidationResult.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());
}
