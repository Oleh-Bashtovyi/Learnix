using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Users.Commands.AdminAssignRole;

internal sealed class AdminAssignRoleValidator : AbstractValidator<AdminAssignRoleCommand>
{
    public AdminAssignRoleValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role must not be empty.")
            .Must(r => Roles.All.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.");
    }
}
