using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.CreateTestLesson;

internal sealed class CreateTestLessonCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseSectionCommandHandler<CreateTestLessonCommand, Result<Guid>>(
        courseRepository, currentUser, lessonsBySectionId: true)
{
    protected override async Task<Result<Guid>> HandleAsync(
        CreateTestLessonCommand request, Course course, CancellationToken cancellationToken)
    {
        var lesson = TestLesson.Create(
            request.SectionId,
            request.Title,
            request.Description,
            request.AttemptLimit,
            request.CooldownMinutes,
            request.PassingThreshold,
            request.ReviewMode);

        lesson.ReplaceQuestions(request.Questions);

        course.AddLesson(lesson);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(lesson.Id);
    }
}
