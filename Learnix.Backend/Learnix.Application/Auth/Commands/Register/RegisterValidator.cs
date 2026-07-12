using FluentValidation;
using Learnix.Application.Auth.Validation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Auth.Commands.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .ValidEmail();

        RuleFor(x => x.Password)
            .ValidPassword();

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(UserConstants.FirstNameMaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(UserConstants.LastNameMaxLength);
    }
}
