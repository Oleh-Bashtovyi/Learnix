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
using Learnix.Domain.Entities;
using Learnix.Domain.ValueObjects;
using MediatR;

namespace Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;

public sealed class SubmitTestAttemptCommandHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ITestAttemptRepository testAttemptRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitTestAttemptCommand, Result<SubmitTestAttemptResponse>>
{
    public async Task<Result<SubmitTestAttemptResponse>> Handle(
        SubmitTestAttemptCommand request,
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

        var priorAttempts = await testAttemptRepository.ListAsync(
            new TestAttemptsByStudentAndLessonSpecification(studentId, request.LessonId),
            cancellationToken);

        if (testLesson.AttemptLimit.HasValue && priorAttempts.Count >= testLesson.AttemptLimit.Value)
            return Result.Fail(new ForbiddenError(TestAttemptMessages.AttemptLimitReached));

        if (testLesson.CooldownMinutes.HasValue && priorAttempts.Count > 0)
        {
            var latest = priorAttempts[0]; // ordered desc by AttemptNumber
            if (latest.SubmittedAt.HasValue)
            {
                var cooldownEndsAt = latest.SubmittedAt.Value.AddMinutes(testLesson.CooldownMinutes.Value);
                if (DateTime.UtcNow < cooldownEndsAt)
                {
                    var remaining = (int)Math.Ceiling((cooldownEndsAt - DateTime.UtcNow).TotalMinutes);
                    return Result.Fail(new ForbiddenError(TestAttemptMessages.CooldownActive(remaining)));
                }
            }
        }

        var studentAnswers = request.Answers
            .Select(a => new StudentAnswer(a.QuestionOrder, a.SelectedOptionOrders, a.TextValue))
            .ToList();

        var score = testLesson.Score(studentAnswers);
        var maxScore = testLesson.MaxScore;
        var attemptNumber = priorAttempts.Count + 1;

        var attempt = TestAttempt.Create(request.CourseId, request.LessonId, studentId, attemptNumber);
        attempt.Submit(studentAnswers, score, maxScore, testLesson.PassingThreshold);

        await testAttemptRepository.AddAsync(attempt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var answerMap = studentAnswers.ToDictionary(a => a.QuestionOrder);
        var questionResults = testLesson.Questions
            .Select(q =>
            {
                var hasAnswer = answerMap.TryGetValue(q.Order, out var ans);
                return new QuestionResultDto(q.Order, hasAnswer && q.IsAnsweredCorrectly(ans!));
            })
            .ToList();

        return Result.Ok(new SubmitTestAttemptResponse(
            attempt.Id,
            attempt.AttemptNumber,
            attempt.Score!.Value,
            attempt.MaxScore!.Value,
            attempt.Passed!.Value,
            attempt.SubmittedAt!.Value,
            questionResults));
    }
}
