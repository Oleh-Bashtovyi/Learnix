using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Sections.Commands.DeleteSection;

/// <remarks>
/// Loads every lesson of the course: <see cref="Course.RemoveSection"/> re-checks the
/// published-course invariants, and "at least one visible lesson" is a course-wide condition
/// that cannot be evaluated from the target section alone.
/// </remarks>
internal sealed class DeleteSectionCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseSectionCommandHandler<DeleteSectionCommand, Result>(
        courseRepository, currentUser, includeAllLessons: true)
{
    protected override async Task<Result> HandleAsync(
        DeleteSectionCommand request, Course course, CancellationToken ct)
    {
        course.RemoveSection(request.SectionId);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
