using FluentResults;
using FluentValidation.Results;

namespace Learnix.Application.Common.Errors;

/// <summary>
/// Carries FluentValidation's ValidationResult so consumers can project it
/// into whatever shape they need (ProblemDetails dictionary, logs, etc.).
/// </summary>
public sealed class ValidationError : Error
{
    public ValidationResult ValidationResult { get; }

    public ValidationError(ValidationResult validationResult)
        : base("One or more validation errors occurred.")
    {
        ValidationResult = validationResult;
    }

    public IReadOnlyDictionary<string, string[]> ToDictionary()
        => ValidationResult.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());
}
