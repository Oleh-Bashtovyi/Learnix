using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Sections.Commands.UpdateSectionTitle;

internal sealed class UpdateSectionTitleCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseSectionCommandHandler<UpdateSectionTitleCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        UpdateSectionTitleCommand request, Course course, CancellationToken ct)
    {
        var section = course.FindSection(request.SectionId);

        section.UpdateTitle(request.Title);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
