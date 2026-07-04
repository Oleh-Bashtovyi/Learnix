using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.CreateTestLesson;

internal sealed class CreateTestLessonCommandHandler(
    ICourseRepository courseRepository,
    ILessonRepository lessonRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseSectionCommandHandler<CreateTestLessonCommand, Result<Guid>>(courseRepository, currentUser)
{
    protected override async Task<Result<Guid>> HandleAsync(
        CreateTestLessonCommand request, Course course, CancellationToken ct)
    {
        var displayOrder = await lessonRepository.GetMaxDisplayOrderAsync(request.SectionId, ct) + 1;

        var lesson = TestLesson.Create(
            request.SectionId,
            request.Title,
            displayOrder,
            request.Description,
            request.AttemptLimit,
            request.CooldownMinutes,
            request.PassingThreshold);

        lesson.ReplaceQuestions(request.Questions);

        await lessonRepository.AddAsync(lesson, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok(lesson.Id);
    }
}
