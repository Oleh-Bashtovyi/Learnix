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
    ITestVersionRepository testVersionRepository,
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

        // The questions belong to a version, not to the lesson, so a student's attempt can be pinned to
        // the edition they were served (ADR-BACK-LMS-006). A brand-new test starts at version 1.
        var version = TestVersion.Create(lesson.Id, request.Questions);
        testVersionRepository.Add(version);

        lesson.SetCurrentVersion(version);

        course.AddLesson(lesson);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(lesson.Id);
    }
}
