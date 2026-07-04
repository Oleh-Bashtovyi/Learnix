using FluentValidation;
using Learnix.Application.Auth.Constants;

namespace Learnix.Application.Auth.Validation;

public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> ValidPassword<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MinimumLength(AuthValidationConstants.PasswordMinLength)
            .MaximumLength(AuthValidationConstants.PasswordMaxLength)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
