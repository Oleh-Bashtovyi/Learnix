using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Users.Commands.AdminRemoveRole;

internal sealed class AdminRemoveRoleValidator : AbstractValidator<AdminRemoveRoleCommand>
{
    public AdminRemoveRoleValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role must not be empty.")
            .Must(r => Roles.All.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.")
            .Must(r => !r.Equals(Roles.Student, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"The '{Roles.Student}' role is the base role and cannot be removed.");
    }
}
