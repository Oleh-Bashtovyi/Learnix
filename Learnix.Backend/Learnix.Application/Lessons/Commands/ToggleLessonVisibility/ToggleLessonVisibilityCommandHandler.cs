using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.ToggleLessonVisibility;

internal sealed class ToggleLessonVisibilityCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseCommandHandler<ToggleLessonVisibilityCommand, Result>(courseRepository, currentUser, includeLessons: true)
{
    protected override async Task<Result> HandleAsync(
        ToggleLessonVisibilityCommand request, Course course, CancellationToken ct)
    {
        var lesson = course.TryGetLesson(request.LessonId);

        if (lesson is null)
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        course.ToggleLessonVisibility(lesson, request.IsVisible);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
