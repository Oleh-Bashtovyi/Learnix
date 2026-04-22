using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.UpdateVideoLesson;

internal sealed class UpdateVideoLessonCommandHandler(
    ICourseRepository courseRepository,
    ILessonRepository lessonRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseCommandHandler<UpdateVideoLessonCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        UpdateVideoLessonCommand request, Course course, CancellationToken ct)
    {
        var lesson = await lessonRepository.GetLessonOfTypeByIdAsync<VideoLesson>(request.LessonId, forUpdate: true, ct);

        if (lesson is null)
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        if (!course.SectionExists(lesson.SectionId))
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        lesson.UpdateVideo(
            request.Title,
            request.VideoUrl,
            request.Description,
            request.DurationSeconds);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
