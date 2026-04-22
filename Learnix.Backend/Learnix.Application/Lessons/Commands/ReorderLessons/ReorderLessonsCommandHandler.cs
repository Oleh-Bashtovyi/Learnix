using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.ReorderLessons;

internal sealed class ReorderLessonsCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseSectionCommandHandler<ReorderLessonsCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        ReorderLessonsCommand request, Course course, CancellationToken ct)
    {
        var pairs = request.Items.Select(i => (i.Id, i.Order)).ToList();

        course.ReorderLessons(request.SectionId, pairs);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
