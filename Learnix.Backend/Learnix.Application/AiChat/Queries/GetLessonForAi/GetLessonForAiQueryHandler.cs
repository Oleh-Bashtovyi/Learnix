using FluentResults;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Application.TestAttempts.Specifications;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetLessonForAi;

internal sealed class GetLessonForAiQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ITestAttemptRepository testAttemptRepository)
    : IRequestHandler<GetLessonForAiQuery, Result<LessonForAiDto>>
{
    private const string VideoUnavailableReason =
        "This is a video lesson. There is no transcript and the assistant cannot watch video, "
        + "so only the title and the instructor's description are known.";

    private const string TestNotSubmittedReason =
        "This is a test. Its questions and answers are withheld until the student submits an attempt.";

    private const string TestReviewableReason =
        "This is a test the student has already submitted. The questions are not included here — "
        + "call " + ChatToolNames.GetMyTestReview + " to go through their answers.";

    public async Task<Result<LessonForAiDto>> Handle(
        GetLessonForAiQuery request,
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

        var lesson = await lessonRepository.GetVisibleLessonInCourseAsync(
            request.CourseId, request.LessonId, cancellationToken);

        if (lesson is null)
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        return lesson switch
        {
            VideoLesson v => Result.Ok(MapVideo(v)),
            PostLesson p => Result.Ok(MapPost(p)),
            TestLesson t => Result.Ok(await MapTestAsync(t, studentId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unknown lesson type for lesson {lesson.Id}.")
        };
    }

    private static LessonForAiDto MapVideo(VideoLesson video) => new(
        video.Id, video.Title, video.LessonType,
        ContentAvailable: false,
        Description: video.Description,
        DurationSeconds: video.DurationSeconds,
        ContentUnavailableReason: VideoUnavailableReason);

    private static LessonForAiDto MapPost(PostLesson post)
    {
        // Tool results are persisted and replayed inside the context window on every later turn, so an
        // uncapped lesson body is paid for repeatedly.
        var truncated = post.Content.Length > AiChatToolLimits.LessonContentMaxLength;

        var content = truncated
            ? post.Content[..AiChatToolLimits.LessonContentMaxLength]
            : post.Content;

        return new LessonForAiDto(
            post.Id, post.Title, post.LessonType,
            ContentAvailable: true,
            Content: content,
            ContentTruncated: truncated);
    }

    /// <summary>Questions are never read here — only counted. See ADR-BACK-CHAT-012.</summary>
    private async Task<LessonForAiDto> MapTestAsync(TestLesson test, Guid studentId, CancellationToken cancellationToken)
    {
        var submittedAttempts = await testAttemptRepository.CountAsync(
            new TestAttemptsByStudentAndLessonSpecification(studentId, test.Id), cancellationToken);

        // Only worth asking when there is something to review at all.
        var hasOpenAttempt = submittedAttempts > 0 && await testAttemptRepository.AnyAsync(
            new InProgressAttemptByStudentAndLessonSpecification(studentId, test.Id), cancellationToken);

        var reviewAvailable = submittedAttempts > 0 && !hasOpenAttempt;

        return new LessonForAiDto(
            test.Id, test.Title, test.LessonType,
            ContentAvailable: false,
            Description: test.Description,
            Test: new TestInfoDto(
                test.QuestionsCount,
                test.PassingThreshold,
                test.AttemptLimit,
                test.CooldownMinutes,
                submittedAttempts,
                reviewAvailable),
            ContentUnavailableReason: reviewAvailable ? TestReviewableReason : TestNotSubmittedReason);
    }
}
