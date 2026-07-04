using FluentValidation;
using Learnix.Application.Auth.Constants;
using Learnix.Application.Auth.Validation;

namespace Learnix.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(AuthValidationConstants.EmailMaxLength);

        RuleFor(x => x.Token).NotEmpty();

        RuleFor(x => x.NewPassword).ValidPassword();
    }
}
