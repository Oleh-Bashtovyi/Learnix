using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Sections.Commands.UpdateSectionTitle;

internal sealed class UpdateSectionTitleCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateSectionTitleCommand, Result>
{
    public async Task<Result> Handle(UpdateSectionTitleCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("Not authenticated."));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdWithStructureSpecification(request.CourseId, forUpdate: true), cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course {request.CourseId} not found."));

        if (course.InstructorId != currentUser.UserId && !currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("You are not the owner of this course."));

        course.UpdateSectionTitle(request.SectionId, request.Title);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Ok();
    }
}