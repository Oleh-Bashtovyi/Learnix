using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.TestAttempts.Constants;
using Learnix.Application.TestAttempts.Specifications;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.TestAttempts.Queries.GetTestLesson;

public sealed class GetTestLessonQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ITestAttemptRepository testAttemptRepository)
    : IRequestHandler<GetTestLessonQuery, Result<GetTestLessonResponse>>
{
    public async Task<Result<GetTestLessonResponse>> Handle(
        GetTestLessonQuery request,
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

        var attempts = await testAttemptRepository.ListAsync(
            new TestAttemptsByStudentAndLessonSpecification(studentId, request.LessonId),
            cancellationToken);

        var latest = attempts.Count > 0 ? attempts[0] : null;

        int? cooldownRemaining = null;
        if (testLesson.CooldownMinutes.HasValue && latest?.SubmittedAt.HasValue == true)
        {
            var cooldownEndsAt = latest.SubmittedAt!.Value.AddMinutes(testLesson.CooldownMinutes.Value);
            var remaining = cooldownEndsAt - DateTime.UtcNow;
            if (remaining > TimeSpan.Zero)
                cooldownRemaining = (int)Math.Ceiling(remaining.TotalMinutes);
        }

        var limitReached = testLesson.AttemptLimit.HasValue && attempts.Count >= testLesson.AttemptLimit.Value;
        var canAttempt = !limitReached && cooldownRemaining is null;

        var studentStatus = new StudentTestStatusDto(
            AttemptsUsed: attempts.Count,
            CanAttempt: canAttempt,
            CooldownRemainingMinutes: cooldownRemaining,
            LatestAttempt: latest is null ? null : new LatestAttemptDto(
                latest.Id,
                latest.AttemptNumber,
                latest.Score!.Value,
                latest.MaxScore!.Value,
                latest.Passed!.Value,
                latest.SubmittedAt!.Value));

        var questions = testLesson.Questions
            .OrderBy(q => q.Order)
            .Select(q => new QuestionDto(
                q.Text,
                q.Type,
                q.Order,
                q.Type != QuestionType.TextInput
                    ? q.Options.OrderBy(o => o.Order).Select(o => new QuestionOptionDto(o.Text, o.Order)).ToList()
                    : null))
            .ToList();

        return Result.Ok(new GetTestLessonResponse(
            testLesson.Id,
            testLesson.Title,
            testLesson.Description,
            testLesson.PassingThreshold,
            testLesson.AttemptLimit,
            testLesson.CooldownMinutes,
            studentStatus,
            questions));
    }
}
