using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Commands.ArchiveCourse;

public sealed class ArchiveCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork)
    : CourseCommandHandler<ArchiveCourseCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        ArchiveCourseCommand request, Course course, CancellationToken ct)
    {
        course.Archive();

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
