using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.UpdateTestLesson;

internal sealed class UpdateTestLessonCommandHandler(
    ICourseRepository courseRepository,
    ILessonRepository lessonRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseCommandHandler<UpdateTestLessonCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        UpdateTestLessonCommand request, Course course, CancellationToken cancellationToken)
    {
        var lesson = await lessonRepository.GetLessonOfTypeByIdAsync<TestLesson>(request.LessonId, forUpdate: true, cancellationToken);

        if (lesson is null)
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        if (!course.SectionExists(lesson.SectionId))
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        lesson.UpdateTest(
            request.Title,
            request.Description,
            request.AttemptLimit,
            request.CooldownMinutes,
            request.PassingThreshold,
            request.ReviewMode,
            request.Questions);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
