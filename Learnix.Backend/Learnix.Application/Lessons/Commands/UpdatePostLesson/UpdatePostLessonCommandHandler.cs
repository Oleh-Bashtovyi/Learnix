using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.UpdatePostLesson;

internal sealed class UpdatePostLessonCommandHandler(
    ICourseRepository courseRepository,
    ILessonRepository lessonRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseCommandHandler<UpdatePostLessonCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        UpdatePostLessonCommand request, Course course, CancellationToken cancellationToken)
    {
        var lesson = await lessonRepository.GetLessonOfTypeByIdAsync<PostLesson>(request.LessonId, forUpdate: true, cancellationToken);

        if (lesson is null)
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        if (!course.SectionExists(lesson.SectionId))
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        lesson.UpdatePost(request.Title, request.Content);

        // The lesson is already tracked (forUpdate: true); mutating it is enough. Calling AddAsync here
        // would flip it to Added and make SaveChanges INSERT a row that already exists — a duplicate-key
        // 500, which is what the UpdatePostLessonTests integration test surfaced.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
