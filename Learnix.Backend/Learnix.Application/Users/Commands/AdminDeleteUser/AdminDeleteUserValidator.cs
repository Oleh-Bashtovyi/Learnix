using FluentValidation;

namespace Learnix.Application.Users.Commands.AdminDeleteUser;

internal sealed class AdminDeleteUserValidator : AbstractValidator<AdminDeleteUserCommand>
{
    public AdminDeleteUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
