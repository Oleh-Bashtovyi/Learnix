using FluentValidation;

namespace Learnix.Application.Users.Commands.AdminRecoverUser;

internal sealed class AdminRecoverUserValidator : AbstractValidator<AdminRecoverUserCommand>
{
    public AdminRecoverUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
