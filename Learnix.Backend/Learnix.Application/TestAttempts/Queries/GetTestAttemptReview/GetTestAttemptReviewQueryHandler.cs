using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.TestAttempts.Constants;
using Learnix.Application.TestAttempts.Services;
using Learnix.Application.TestAttempts.Specifications;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;
using MediatR;

namespace Learnix.Application.TestAttempts.Queries.GetTestAttemptReview;

/// <summary>
/// The student's own record of a submitted attempt. The answers were always persisted — this is what
/// was missing to let anyone but the AI tutor read them back.
/// </summary>
internal sealed class GetTestAttemptReviewQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ITestAttemptRepository testAttemptRepository)
    : IRequestHandler<GetTestAttemptReviewQuery, Result<TestAttemptReviewResponse>>
{
    public async Task<Result<TestAttemptReviewResponse>> Handle(
        GetTestAttemptReviewQuery request,
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

        var test = await lessonRepository.GetTestLessonInCourseAsync(
            request.CourseId, request.LessonId, cancellationToken);

        if (test is null)
            return Result.Fail(new NotFoundError(TestAttemptMessages.TestLessonNotFound));

        // Scoped to the caller, so another student's attempt id resolves to nothing rather than to a 403 —
        // which would confirm that the attempt exists.
        var attempt = await testAttemptRepository.FirstOrDefaultAsync(
            new AttemptByIdAndStudentSpecification(request.AttemptId, studentId),
            cancellationToken);

        if (attempt is null || attempt.TestLessonId != request.LessonId)
            return Result.Fail(new NotFoundError(TestAttemptMessages.AttemptNotFound));

        // An attempt still in progress has no answers to review, and reviewing it would mean handing the
        // student the marking scheme mid-test.
        if (!attempt.IsSubmitted)
            return Result.Fail(new ConflictError(TestAttemptMessages.AttemptNotSubmitted));

        return Result.Ok(Map(test, attempt));
    }

    private static TestAttemptReviewResponse Map(TestLesson test, TestAttempt attempt)
    {
        var mode = test.ReviewMode;
        var answersByQuestion = attempt.Answers.ToDictionary(a => a.QuestionOrder);

        var questions = TestReviewPolicy.ShowsAnswers(mode)
            ? test.Questions
                .OrderBy(q => q.Order)
                .Select(q => MapQuestion(q, answersByQuestion.GetValueOrDefault(q.Order), mode))
                .ToList()
            : [];

        return new TestAttemptReviewResponse(
            attempt.Id,
            attempt.AttemptNumber,
            attempt.Score!.Value,
            attempt.MaxScore!.Value,
            attempt.Passed!.Value,
            attempt.StartedAt,
            attempt.SubmittedAt!.Value,
            mode,
            questions);
    }

    private static ReviewedQuestionDto MapQuestion(
        Question question,
        StudentAnswer? answer,
        TestReviewMode mode)
    {
        var isChoice = question.Type is QuestionType.SingleChoice or QuestionType.MultipleChoice;
        var showCorrectAnswers = TestReviewPolicy.ShowsCorrectAnswers(mode);

        var options = isChoice
            ? question.Options
                .OrderBy(o => o.Order)
                .Select(o => new ReviewedOptionDto(
                    o.Order,
                    o.Text,
                    showCorrectAnswers ? o.IsCorrect : null))
                .ToList()
            : null;

        bool? isCorrect = TestReviewPolicy.ShowsCorrectness(mode)
            ? answer is not null && question.IsAnsweredCorrectly(answer)
            : null;

        return new ReviewedQuestionDto(
            Order: question.Order,
            Text: question.Text,
            Type: question.Type,
            Answered: answer is not null,
            IsCorrect: isCorrect,
            Options: options,
            StudentSelectedOptionOrders: isChoice ? answer?.SelectedOptionOrders : null,
            StudentTextAnswer: isChoice ? null : answer?.TextValue,
            CorrectTextAnswer: showCorrectAnswers && !isChoice ? question.TextAnswer?.CorrectAnswer : null);
    }
}
