using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.DeleteLesson;

internal sealed class DeleteLessonCommandHandler(
    ICourseRepository courseRepository,
    ILessonRepository lessonRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseCommandHandler<DeleteLessonCommand, Result>(courseRepository, currentUser, includeLessons: true)
{
    protected override async Task<Result> HandleAsync(
        DeleteLessonCommand request, Course course, CancellationToken ct)
    {
        var lesson = course.TryGetLesson(request.LessonId);

        if (lesson is null)
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        course.RemoveLesson(lesson);

        await lessonRepository.DeleteAsync(lesson, ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
