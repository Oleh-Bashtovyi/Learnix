using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.LessonProgress.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.TestAttempts.Constants;
using Learnix.Application.TestAttempts.Services;
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
    ICourseCompletionService courseCompletion,
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
            lessonProgressRepository.Add(progress);
            progress.MarkCompleted();
        }
        else if (!existingProgress.IsCompleted)
        {
            existingProgress.MarkCompleted();
        }

        if (isFirstCompletion)
        {
            await courseCompletion.TryCompleteAsync(
                studentId, request.CourseId, justCompletedLessonId: request.LessonId, cancellationToken);
        }

        // One commit for the attempt, the lesson, the enrollment and the certificate.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The results screen is the first — and, before the review existed, the only — place the answers
        // are revealed, so it obeys the instructor's review mode like every other. Gating the review
        // alone would be theatre: the student would simply read the answers off this screen.
        var mode = testLesson.ReviewMode;
        var answerMap = studentAnswers.ToDictionary(a => a.QuestionOrder);

        var questionResults = TestReviewPolicy.ShowsAnswers(mode)
            ? testLesson.Questions
                .Select(q =>
                {
                    var hasAnswer = answerMap.TryGetValue(q.Order, out var ans);

                    bool? isCorrect = TestReviewPolicy.ShowsCorrectness(mode)
                        ? hasAnswer && q.IsAnsweredCorrectly(ans!)
                        : null;

                    var correctOptionOrders = TestReviewPolicy.ShowsCorrectAnswers(mode)
                        && q.Type != QuestionType.TextInput
                            ? q.Options.Where(o => o.IsCorrect).Select(o => o.Order).ToList()
                            : null;

                    var correctTextAnswer = TestReviewPolicy.ShowsCorrectAnswers(mode)
                        && q.Type == QuestionType.TextInput
                            ? q.TextAnswer?.CorrectAnswer
                            : null;

                    return new QuestionResultDto(q.Order, isCorrect, correctOptionOrders, correctTextAnswer);
                })
                .ToList()
            : [];

        return Result.Ok(new SubmitTestAttemptResponse(
            attempt.Id,
            attempt.AttemptNumber,
            attempt.Score!.Value,
            attempt.MaxScore!.Value,
            attempt.Passed!.Value,
            attempt.SubmittedAt!.Value,
            mode,
            questionResults));
    }
}
