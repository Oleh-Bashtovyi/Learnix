using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.CreateVideoLesson;

internal sealed class CreateVideoLessonCommandHandler(
    ICourseRepository courseRepository,
    ILessonRepository lessonRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseSectionCommandHandler<CreateVideoLessonCommand, Result<Guid>>(courseRepository, currentUser)
{
    protected override async Task<Result<Guid>> HandleAsync(
        CreateVideoLessonCommand request, Course course, CancellationToken ct)
    {
        var commitResult = await blobStorage.CommitUploadAsync(
            request.VideoBlobPath, UploadTarget.LessonVideo, ct);

        if (commitResult.IsFailed)
            return Result.Fail(commitResult.Errors);

        var displayOrder = await lessonRepository.GetMaxDisplayOrderAsync(request.SectionId, ct) + 1;

        var lesson = VideoLesson.Create(
            request.SectionId,
            request.Title,
            displayOrder,
            commitResult.Value.BlobPath,
            request.Description,
            request.DurationSeconds);

        await lessonRepository.AddAsync(lesson, ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok(lesson.Id);
    }
}
