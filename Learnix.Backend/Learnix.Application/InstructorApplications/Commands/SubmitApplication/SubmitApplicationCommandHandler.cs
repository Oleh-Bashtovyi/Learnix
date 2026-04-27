using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.InstructorApplications.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.InstructorApplications.Commands.SubmitApplication;

internal sealed class SubmitApplicationCommandHandler(
    IInstructorApplicationRepository repo,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : IRequestHandler<SubmitApplicationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (currentUser.IsInRole(Roles.Instructor) || currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ConflictError("You are already an instructor."));

        var existing = await repo.FirstOrDefaultAsync(
            new ApplicationByUserIdSpecification(currentUser.UserId.Value, forUpdate: true),
            cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == ApplicationStatus.Pending)
                return Result.Fail(new ConflictError("You already have a pending application."));

            if (existing.Status == ApplicationStatus.Approved)
                return Result.Fail(new ConflictError("Your application has already been approved."));

            // Rejected — allow resubmission by updating the existing record
            existing.Resubmit(request.MotivationText, request.PortfolioUrl);
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Ok(existing.Id);
        }

        var application = InstructorApplication.Create(
            currentUser.UserId.Value,
            request.MotivationText,
            request.PortfolioUrl);

        await repo.AddAsync(application, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Ok(application.Id);
    }
}
