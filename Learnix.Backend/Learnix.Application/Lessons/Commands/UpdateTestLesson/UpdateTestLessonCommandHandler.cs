using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.Lessons.Specifications;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.TestAttempts.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.UpdateTestLesson;

internal sealed class UpdateTestLessonCommandHandler(
    ICourseRepository courseRepository,
    ILessonRepository lessonRepository,
    ITestVersionRepository testVersionRepository,
    ITestAttemptRepository testAttemptRepository,
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
            request.ReviewMode);

        await ApplyQuestionsAsync(lesson, request, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <summary>
    /// Lands the submitted questions on a version, choosing between three outcomes (ADR-BACK-LMS-006):
    /// leave the current version alone when the questions did not change, reuse its row when nothing
    /// has been attempted against it, and branch a new one when something has.
    /// </summary>
    private async Task ApplyQuestionsAsync(
        TestLesson lesson, UpdateTestLessonCommand request, CancellationToken cancellationToken)
    {
        var current = lesson.CurrentVersionId is null
            ? null
            : await testVersionRepository.FirstOrDefaultAsync(
                new TestVersionByIdSpecification(lesson.CurrentVersionId.Value, forUpdate: true),
                cancellationToken);

        // A test lesson without a version predates nothing and should not exist, but a missing row is
        // not a reason to reject the instructor's edit — give it the version it should have had.
        if (current is null)
        {
            var created = TestVersion.Create(lesson.Id, request.Questions);
            testVersionRepository.Add(created);
            lesson.SetCurrentVersion(created);
            return;
        }

        // Saving the editor without touching a question is the common case — an instructor renaming the
        // test or moving the passing threshold. Branching there would fill the table with editions that
        // differ in nothing.
        if (current.Matches(request.Questions))
            return;

        var isAttempted = await testAttemptRepository.AnyAsync(
            new AttemptsByTestVersionSpecification(current.Id), cancellationToken);

        if (!isAttempted)
        {
            // Nobody is holding these questions, so there is no history to preserve: the version keeps
            // its number and its row, and a test that never gets sat never grows past one version.
            current.Overwrite(request.Questions);
            lesson.SetCurrentVersion(current);
            return;
        }

        var next = current.Branch(request.Questions);
        testVersionRepository.Add(next);
        lesson.SetCurrentVersion(next);
    }
}
