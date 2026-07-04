using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Constants;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Users.Commands.AdminAssignRole;

internal sealed class AdminAssignRoleCommandHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IUserRoleService roleService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdminAssignRoleCommand, Result>
{
    public async Task<Result> Handle(AdminAssignRoleCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(UserMessages.OnlyAdminsCanChangeRoles));

        var user = await userRepository.FirstOrDefaultAsync(
            new AdminUserByIdSpecification(request.UserId, forUpdate: true),
            cancellationToken);

        if (user is null)
            return Result.Fail(new NotFoundError(CommonMessages.UserNotFoundById(request.UserId)));

        var currentRoles = await roleService.GetRolesAsync(request.UserId, cancellationToken);
        if (currentRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            return Result.Fail(new ConflictError(CommonMessages.UserAlreadyHasRole(request.Role)));

        await roleService.AssignRoleAsync(request.UserId, request.Role, cancellationToken);

        user.RaiseRoleChanged(request.Role, assigned: true);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
