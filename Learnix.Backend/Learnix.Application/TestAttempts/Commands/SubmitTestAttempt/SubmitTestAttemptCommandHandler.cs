using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.LessonProgress.Specifications;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.TestAttempts.Constants;
using Learnix.Application.TestAttempts.Specifications;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;
using MediatR;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;

public sealed class SubmitTestAttemptCommandHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ILessonProgressRepository lessonProgressRepository,
    ITestAttemptRepository testAttemptRepository,
    ICertificateRepository certificateRepository,
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
            var latest = priorAttempts[0];
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

        var attempt = Learnix.Domain.Entities.TestAttempt.Create(
            request.CourseId, request.LessonId, studentId, attemptNumber);
        attempt.Submit(studentAnswers, score, maxScore, testLesson.PassingThreshold);
        await testAttemptRepository.AddAsync(attempt, cancellationToken);

        // Auto-complete test lesson progress on first submission
        var existingProgress = await lessonProgressRepository.FirstOrDefaultAsync(
            new LessonProgressByStudentAndLessonSpecification(studentId, request.LessonId, forUpdate: true),
            cancellationToken);

        var isFirstCompletion = existingProgress is null || !existingProgress.IsCompleted;

        if (existingProgress is null)
        {
            var progress = LessonProgressEntity.Create(request.CourseId, request.LessonId, studentId);
            progress.MarkCompleted();
            await lessonProgressRepository.AddAsync(progress, cancellationToken);
        }
        else if (!existingProgress.IsCompleted)
        {
            existingProgress.MarkCompleted();
        }

        if (isFirstCompletion)
            await TryIssueCertificateAsync(studentId, request.CourseId, cancellationToken);

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

    private async Task TryIssueCertificateAsync(Guid studentId, Guid courseId, CancellationToken ct)
    {
        var visibleCount = await lessonRepository.GetVisibleLessonCountAsync(courseId, ct);
        if (visibleCount == 0) return;

        var completedCount = await lessonProgressRepository.CountAsync(
            new CompletedLessonCountByStudentAndCourseSpecification(studentId, courseId), ct);

        if (completedCount + 1 < visibleCount) return;

        var enrollment = await enrollmentRepository.FirstOrDefaultAsync(
            new EnrollmentByStudentAndCourseSpecification(studentId, courseId, forUpdate: true), ct);

        if (enrollment is null || enrollment.Status == EnrollmentStatus.Completed) return;

        enrollment.MarkCompleted();

        var cert = Learnix.Domain.Entities.Certificate.Create(
            courseId, studentId, enrollment.Id, GenerateCertificateCode());

        await certificateRepository.AddAsync(cert, ct);
    }

    private static string GenerateCertificateCode()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"CERT-{date}-{random}";
    }
}
