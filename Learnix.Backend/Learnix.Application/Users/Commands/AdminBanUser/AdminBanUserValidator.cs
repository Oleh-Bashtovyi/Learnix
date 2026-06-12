using FluentValidation;

namespace Learnix.Application.Users.Commands.AdminBanUser;

internal sealed class AdminBanUserValidator : AbstractValidator<AdminBanUserCommand>
{
    public AdminBanUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
