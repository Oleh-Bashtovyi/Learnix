using FluentResults;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.TestAttempts.Services;
using Learnix.Application.TestAttempts.Specifications;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetTestReviewForAi;

internal sealed class GetTestReviewForAiQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ITestAttemptRepository testAttemptRepository)
    : IRequestHandler<GetTestReviewForAiQuery, Result<TestReviewForAiDto>>
{
    public async Task<Result<TestReviewForAiDto>> Handle(
        GetTestReviewForAiQuery request,
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
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        // An attempt that is still open must not be coached. The student already saw the answers when they
        // submitted an earlier one, but restating them into a live attempt is not tutoring.
        var hasOpenAttempt = await testAttemptRepository.AnyAsync(
            new InProgressAttemptByStudentAndLessonSpecification(studentId, request.LessonId),
            cancellationToken);

        if (hasOpenAttempt)
            return Result.Fail(new ConflictError(AiChatMessages.TestAttemptInProgress));

        // Ordered by attempt number descending, submitted only.
        var attempts = await testAttemptRepository.ListAsync(
            new TestAttemptsByStudentAndLessonSpecification(studentId, request.LessonId),
            cancellationToken);

        var latest = attempts.FirstOrDefault();

        if (latest is null)
            return Result.Fail(new NotFoundError(AiChatMessages.TestNotSubmitted));

        // The tutor may not see what the student may not see. This used to be justified the other way
        // round — the platform revealed everything at submission time, so there was nothing left to
        // withhold — but the instructor can now choose otherwise, and a tutor that recites the answers
        // the instructor hid would be the easiest way around the setting.
        if (!TestReviewPolicy.ShowsAnswers(test.ReviewMode))
            return Result.Fail(new ForbiddenError(AiChatMessages.TestReviewNotAllowed));

        return Result.Ok(Map(test, latest));
    }

    private static TestReviewForAiDto Map(TestLesson test, TestAttempt attempt)
    {
        var answersByQuestion = attempt.Answers.ToDictionary(a => a.QuestionOrder);

        var questions = test.Questions
            .OrderBy(q => q.Order)
            .Select(q => MapQuestion(q, answersByQuestion.GetValueOrDefault(q.Order), test.ReviewMode))
            .ToList();

        return new TestReviewForAiDto(
            test.Id,
            test.Title,
            attempt.AttemptNumber,
            attempt.Score!.Value,
            attempt.MaxScore!.Value,
            attempt.Passed!.Value,
            attempt.SubmittedAt!.Value,
            questions);
    }

    private static QuestionReviewDto MapQuestion(
        Question question,
        StudentAnswer? answer,
        TestReviewMode mode)
    {
        var isChoice = question.Type is QuestionType.SingleChoice or QuestionType.MultipleChoice;
        var showCorrectAnswers = TestReviewPolicy.ShowsCorrectAnswers(mode);

        var options = isChoice
            ? question.Options
                .Select(o => new OptionReviewDto(o.Order, o.Text, showCorrectAnswers && o.IsCorrect))
                .ToList()
            : null;

        return new QuestionReviewDto(
            Order: question.Order,
            Text: question.Text,
            Type: question.Type,
            Answered: answer is not null,
            IsCorrect: TestReviewPolicy.ShowsCorrectness(mode)
                       && answer is not null
                       && question.IsAnsweredCorrectly(answer),
            Options: options,
            StudentSelectedOptionOrders: isChoice ? answer?.SelectedOptionOrders : null,
            CorrectTextAnswer: showCorrectAnswers ? question.TextAnswer?.CorrectAnswer : null,
            StudentTextAnswer: isChoice ? null : answer?.TextValue);
    }
}
