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
using MediatR;

namespace Learnix.Application.TestAttempts.Queries.GetMyTestAttempts;

public sealed class GetMyTestAttemptsQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ITestAttemptRepository testAttemptRepository)
    : IRequestHandler<GetMyTestAttemptsQuery, Result<IReadOnlyList<TestAttemptSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<TestAttemptSummaryDto>>> Handle(
        GetMyTestAttemptsQuery request,
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

        var isTestInCourse = await lessonRepository.IsLessonInCourseAsync(
            request.CourseId, request.LessonId, cancellationToken);

        if (!isTestInCourse)
            return Result.Fail(new NotFoundError(TestAttemptMessages.TestLessonNotFound));

        var attempts = await testAttemptRepository.ListAsync(
            new TestAttemptsByStudentAndLessonSpecification(studentId, request.LessonId),
            cancellationToken);

        var result = attempts
            .Select(a => new TestAttemptSummaryDto(
                a.Id,
                a.AttemptNumber,
                a.Score!.Value,
                a.MaxScore!.Value,
                a.Passed!.Value,
                a.StartedAt,
                a.SubmittedAt!.Value))
            .ToList();

        return Result.Ok<IReadOnlyList<TestAttemptSummaryDto>>(result);
    }
}
