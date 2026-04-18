using FluentValidation;
using Learnix.Application.Auth.Constants;
using Learnix.Domain.Constants;

namespace Learnix.Application.Auth.Commands.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(AuthValidationConstants.EmailMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(AuthValidationConstants.PasswordMinLength)
            .MaximumLength(AuthValidationConstants.PasswordMaxLength)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(UserConstants.FirstNameMaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(UserConstants.LastNameMaxLength);
    }
}
