using FluentValidation;
using Learnix.Application.Auth.Constants;

namespace Learnix.Application.Auth.Commands.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(AuthValidationConstants.EmailMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(AuthValidationConstants.PasswordMaxLength);
    }
}
