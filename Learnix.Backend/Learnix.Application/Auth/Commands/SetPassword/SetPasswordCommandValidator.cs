using FluentValidation;
using Learnix.Application.Auth.Validation;

namespace Learnix.Application.Auth.Commands.SetPassword;

public sealed class SetPasswordCommandValidator : AbstractValidator<SetPasswordCommand>
{
    public SetPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).ValidPassword();
    }
}
