using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.TestAttempts.Constants;
using Learnix.Application.TestAttempts.Specifications;
using MediatR;

namespace Learnix.Application.TestAttempts.Commands.StartTestAttempt;

public sealed class StartTestAttemptCommandHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ITestAttemptRepository testAttemptRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<StartTestAttemptCommand, Result<StartTestAttemptResponse>>
{
    public async Task<Result<StartTestAttemptResponse>> Handle(
        StartTestAttemptCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;

        var isEnrolled = await enrollmentRepository.AnyAsync(
            new ActiveEnrollmentByStudentAndCourseSpecification(studentId, request.CourseId),
            cancellationToken);

        if (!isEnrolled)
            return Result.Fail(new ForbiddenError(CommonMessages.NotEnrolledInCourse));

        var testLesson = await lessonRepository.GetTestLessonInCourseAsync(
            request.CourseId, request.LessonId, cancellationToken);

        if (testLesson is null)
            return Result.Fail(new NotFoundError(TestAttemptMessages.TestLessonNotFound));

        // Idempotency: return the existing in-progress attempt if one already exists.
        // This handles the multi-tab case — both tabs get the same attemptId.
        var inProgress = await testAttemptRepository.FirstOrDefaultAsync(
            new InProgressAttemptByStudentAndLessonSpecification(studentId, request.LessonId),
            cancellationToken);

        if (inProgress is not null)
            return Result.Ok(new StartTestAttemptResponse(inProgress.Id, inProgress.AttemptNumber, inProgress.StartedAt));

        // Check limits and cooldown against submitted attempts only
        var submittedAttempts = await testAttemptRepository.ListAsync(
            new TestAttemptsByStudentAndLessonSpecification(studentId, request.LessonId),
            cancellationToken);

        if (testLesson.AttemptLimit.HasValue && submittedAttempts.Count >= testLesson.AttemptLimit.Value)
            return Result.Fail(new ForbiddenError(TestAttemptMessages.AttemptLimitReached));

        var cooldown = EnsureCooldownElapsed(testLesson, submittedAttempts);

        if (cooldown.IsFailed)
            return Result.Fail(cooldown.Errors);

        var attemptNumber = submittedAttempts.Count + 1;
        var attempt = Domain.Entities.TestAttempt.Create(
            request.CourseId, request.LessonId, studentId, attemptNumber);

        try
        {
            await testAttemptRepository.AddAsync(attempt, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            var existingInProgress = await testAttemptRepository.FirstOrDefaultAsync(
                new InProgressAttemptByStudentAndLessonSpecification(studentId, request.LessonId),
                cancellationToken);

            if (existingInProgress is not null)
                return Result.Ok(new StartTestAttemptResponse(existingInProgress.Id, existingInProgress.AttemptNumber, existingInProgress.StartedAt));

            throw;
        }

        return Result.Ok(new StartTestAttemptResponse(attempt.Id, attempt.AttemptNumber, attempt.StartedAt));
    }

    /// <summary>
    /// Fails while the cooldown after the most recent submitted attempt is still running.
    /// A lesson without a cooldown, a student without submitted attempts, or an attempt that was
    /// never submitted all pass through.
    /// </summary>
    private static Result EnsureCooldownElapsed(
        Domain.Entities.TestLesson testLesson,
        IReadOnlyList<Domain.Entities.TestAttempt> submittedAttempts)
    {
        if (!testLesson.CooldownMinutes.HasValue || submittedAttempts.Count == 0)
            return Result.Ok();

        var submittedAt = submittedAttempts[0].SubmittedAt;

        if (!submittedAt.HasValue)
            return Result.Ok();

        var cooldownEndsAt = submittedAt.Value.AddMinutes(testLesson.CooldownMinutes.Value);
        var now = DateTime.UtcNow;

        if (now >= cooldownEndsAt)
            return Result.Ok();

        var remaining = (int)Math.Ceiling((cooldownEndsAt - now).TotalMinutes);

        return Result.Fail(new ForbiddenError(TestAttemptMessages.CooldownActive(remaining)));
    }
}
