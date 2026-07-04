using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Sections.Commands.DeleteSection;

internal sealed class DeleteSectionCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseSectionCommandHandler<DeleteSectionCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        DeleteSectionCommand request, Course course, CancellationToken ct)
    {
        course.RemoveSection(request.SectionId);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
