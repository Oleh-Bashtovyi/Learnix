using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Commands.UnpublishCourse;

public sealed class UnpublishCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork)
    : CourseCommandHandler<UnpublishCourseCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        UnpublishCourseCommand request, Course course, CancellationToken ct)
    {
        course.Unpublish();

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
