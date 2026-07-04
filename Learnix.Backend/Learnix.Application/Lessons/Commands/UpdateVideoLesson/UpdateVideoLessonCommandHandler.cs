using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
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
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseCommandHandler<UpdateVideoLessonCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        UpdateVideoLessonCommand request, Course course, CancellationToken ct)
    {
        var lesson = await lessonRepository.GetLessonOfTypeByIdAsync<VideoLesson>(
                    request.LessonId, forUpdate: true, ct);

        if (lesson is null)
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        if (!course.SectionExists(lesson.SectionId))
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        lesson.UpdateMetadata(request.Title, request.Description, request.DurationSeconds);

        if (request.VideoBlobPath is not null && request.VideoBlobPath != lesson.VideoBlobPath)
        {
            var commitResult = await blobStorage.CommitUploadAsync(
                request.VideoBlobPath, UploadTarget.LessonVideo, ct);

            if (commitResult.IsFailed)
                return Result.Fail(commitResult.Errors);

            lesson.ReplaceVideo(commitResult.Value.BlobPath);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
