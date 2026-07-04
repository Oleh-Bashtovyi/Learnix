using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Sections.Commands.CreateSection;

internal sealed class CreateSectionCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseCommandHandler<CreateSectionCommand, Result<Guid>>(courseRepository, currentUser)
{
    protected override async Task<Result<Guid>> HandleAsync(
        CreateSectionCommand request, Course course, CancellationToken ct)
    {
        var section = course.AddSection(request.Title);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok(section.Id);
    }
}
