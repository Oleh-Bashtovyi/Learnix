using FluentValidation;
using Learnix.Application.Auth.Constants;
using Learnix.Application.Auth.Validation;

namespace Learnix.Application.Auth.Commands.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .ValidEmail();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(AuthValidationConstants.PasswordMaxLength);
    }
}
