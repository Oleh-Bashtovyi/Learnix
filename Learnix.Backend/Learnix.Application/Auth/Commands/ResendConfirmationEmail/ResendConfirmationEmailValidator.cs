using FluentValidation;
using Learnix.Application.Auth.Constants;

namespace Learnix.Application.Auth.Commands.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailValidator : AbstractValidator<ResendConfirmationEmailCommand>
{
    public ResendConfirmationEmailValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(AuthValidationConstants.EmailMaxLength);
    }
}
