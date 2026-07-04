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

namespace Learnix.Application.Users.Commands.AdminRecoverUser;

internal sealed class AdminRecoverUserCommandHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdminRecoverUserCommand, Result>
{
    public async Task<Result> Handle(AdminRecoverUserCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(UserMessages.OnlyAdminsCanRecoverUsers));

        var user = await userRepository.FirstOrDefaultAsync(
            new AdminUserByIdSpecification(request.UserId, includeDeleted: true, forUpdate: true),
            cancellationToken);

        if (user is null)
            return Result.Fail(new NotFoundError(CommonMessages.UserNotFoundById(request.UserId)));

        if (!user.IsDeleted)
            return Result.Fail(new ConflictError(UserMessages.UserIsNotDeleted));

        user.Recover();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
