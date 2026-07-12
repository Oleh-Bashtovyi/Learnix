using FluentValidation;
using Learnix.Application.Auth.Validation;

namespace Learnix.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email)
            .ValidEmail();

        RuleFor(x => x.NewPassword)
            .ValidPassword();

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
