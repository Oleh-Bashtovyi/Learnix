using FluentValidation;

namespace Learnix.Application.Users.Commands.AdminUnbanUser;

internal sealed class AdminUnbanUserValidator : AbstractValidator<AdminUnbanUserCommand>
{
    public AdminUnbanUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
