using FluentValidation;
using Learnix.Application.Auth.Validation;

namespace Learnix.Application.Auth.Commands.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailValidator : AbstractValidator<ResendConfirmationEmailCommand>
{
    public ResendConfirmationEmailValidator()
    {
        RuleFor(x => x.Email)
            .ValidEmail();
    }
}
