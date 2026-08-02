using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.LessonProgress.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;
using MediatR;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.LessonProgress.Commands.MarkLessonComplete;

public sealed class MarkLessonCompleteCommandHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ILessonProgressRepository lessonProgressRepository,
    ICourseCompletionService courseCompletion,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkLessonCompleteCommand, Result<MarkLessonCompleteResponse>>
{
    public async Task<Result<MarkLessonCompleteResponse>> Handle(
        MarkLessonCompleteCommand request,
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
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotInCourse));

        if (lesson is TestLesson testLesson && testLesson.QuestionsCount > 0)
            return Result.Fail(new ForbiddenError(CommonMessages.TestLessonMustBeCompletedBySubmission));

        var progress = await lessonProgressRepository.FirstOrDefaultAsync(
            new LessonProgressByStudentAndLessonSpecification(studentId, request.LessonId, forUpdate: true),
            cancellationToken);

        var wasAlreadyCompleted = progress?.IsCompleted ?? false;

        if (progress is null)
        {
            progress = LessonProgressEntity.Create(request.CourseId, request.LessonId, studentId);
            lessonProgressRepository.Add(progress);
        }

        progress.MarkCompleted();

        if (!wasAlreadyCompleted)
        {
            await courseCompletion.TryCompleteAsync(
                studentId, request.CourseId, justCompletedLessonId: request.LessonId, cancellationToken);
        }

        // One commit for the lesson, the enrollment and the certificate: a caller that gets a
        // success back has all three, and a failure leaves none of them behind.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new MarkLessonCompleteResponse(progress.Id, progress.IsCompleted, progress.CompletedAt));
    }
}
