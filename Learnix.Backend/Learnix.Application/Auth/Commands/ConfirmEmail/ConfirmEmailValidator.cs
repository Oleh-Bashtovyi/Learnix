using FluentValidation;
using Learnix.Application.Auth.Validation;

namespace Learnix.Application.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailValidator()
    {
        RuleFor(x => x.Email)
            .ValidEmail();

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.");
    }
}
