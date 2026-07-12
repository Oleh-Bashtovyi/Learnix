using FluentValidation;
using Learnix.Application.Auth.Constants;

namespace Learnix.Application.Auth.Validation;

public static class EmailRule
{
    public static IRuleBuilderOptions<T, string> ValidEmail<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(AuthValidationConstants.EmailMaxLength);
    }
}
