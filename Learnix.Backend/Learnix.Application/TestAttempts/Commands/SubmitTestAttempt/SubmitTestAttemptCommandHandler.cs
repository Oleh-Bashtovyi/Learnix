using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.LessonProgress.Specifications;
using Learnix.Application.Lessons.Abstractions;
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
    ILessonRepository lessonRepository,
    ILessonProgressRepository lessonProgressRepository,
    ITestAttemptRepository testAttemptRepository,
    ICertificateRepository certificateRepository,
    IEnrollmentRepository enrollmentRepository,
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

        // Load the specific in-progress attempt — verifies ownership implicitly
        var attempt = await testAttemptRepository.FirstOrDefaultAsync(
            new AttemptByIdAndStudentSpecification(request.AttemptId, studentId),
            cancellationToken);

        if (attempt is null)
            return Result.Fail(new NotFoundError(TestAttemptMessages.AttemptNotFound));

        // Guard against double-submit (e.g., two tabs submitting the same attempt)
        if (attempt.IsSubmitted)
            return Result.Fail(new ConflictError(TestAttemptMessages.AttemptAlreadySubmitted));

        // Validate URL params match the attempt to prevent URL manipulation
        if (attempt.CourseId != request.CourseId || attempt.TestLessonId != request.LessonId)
            return Result.Fail(new NotFoundError(TestAttemptMessages.AttemptNotFound));

        var testLesson = await lessonRepository.GetTestLessonInCourseAsync(
            attempt.CourseId, attempt.TestLessonId, cancellationToken);

        if (testLesson is null)
            return Result.Fail(new NotFoundError(TestAttemptMessages.TestLessonNotFound));

        var studentAnswers = request.Answers
            .Select(a => new StudentAnswer(a.QuestionOrder, a.SelectedOptionOrders, a.TextValue))
            .ToList();

        var score = testLesson.Score(studentAnswers);
        var maxScore = testLesson.MaxScore;

        attempt.Submit(studentAnswers, score, maxScore, testLesson.PassingThreshold);

        // Auto-complete lesson progress on first submission
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
                var isCorrect = hasAnswer && q.IsAnsweredCorrectly(ans!);
                var correctOptionOrders = q.Type != QuestionType.TextInput
                    ? q.Options.Where(o => o.IsCorrect).Select(o => o.Order).ToList()
                    : null;
                var correctTextAnswer = q.Type == QuestionType.TextInput
                    ? q.TextAnswer?.CorrectAnswer
                    : null;
                return new QuestionResultDto(q.Order, isCorrect, correctOptionOrders, correctTextAnswer);
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

        var completedCount = await lessonRepository.GetCompletedVisibleLessonCountAsync(studentId, courseId, ct);

        if (completedCount + 1 < visibleCount) return;

        var enrollment = await enrollmentRepository.FirstOrDefaultAsync(
            new EnrollmentByStudentAndCourseSpecification(studentId, courseId, forUpdate: true), ct);

        if (enrollment is null || enrollment.Status == EnrollmentStatus.Completed) return;

        enrollment.MarkCompleted();

        var cert = Domain.Entities.Certificate.Create(
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
