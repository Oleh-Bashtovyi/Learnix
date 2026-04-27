using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.InstructorApplications.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.InstructorApplications.Commands.ApproveApplication;

internal sealed class ApproveApplicationCommandHandler(
    IInstructorApplicationRepository repo,
    IUserRoleService roleService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : IRequestHandler<ApproveApplicationCommand, Result>
{
    public async Task<Result> Handle(ApproveApplicationCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("Only admins can approve applications."));

        var application = await repo.FirstOrDefaultAsync(
            new ApplicationByIdSpecification(request.ApplicationId, forUpdate: true),
            cancellationToken);

        if (application is null)
            return Result.Fail(new NotFoundError(CommonMessages.InstructorApplicationNotFound(request.ApplicationId)));

        if (application.Status != ApplicationStatus.Pending)
            return Result.Fail(new ConflictError("Only pending applications can be approved."));

        application.Approve(currentUser.UserId.Value);

        await roleService.AssignRoleAsync(application.UserId, Roles.Instructor, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Ok();
    }
}
