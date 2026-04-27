using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.LessonProgress.Specifications;
using MediatR;

namespace Learnix.Application.LessonProgress.Commands.MarkLessonComplete;

public sealed class MarkLessonCompleteCommandHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ILessonProgressRepository lessonProgressRepository,
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

        var lessonInCourse = await lessonRepository.IsLessonInCourseAsync(
            request.CourseId, request.LessonId, cancellationToken);

        if (!lessonInCourse)
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotInCourse));

        var progress = await lessonProgressRepository.FirstOrDefaultAsync(
            new LessonProgressByStudentAndLessonSpecification(studentId, request.LessonId, forUpdate: true),
            cancellationToken);

        if (progress is null)
        {
            progress = Learnix.Domain.Entities.LessonProgress.Create(request.CourseId, request.LessonId, studentId);
            progress.MarkCompleted();
            await lessonProgressRepository.AddAsync(progress, cancellationToken);
        }
        else
        {
            progress.MarkCompleted();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new MarkLessonCompleteResponse(progress.Id, progress.IsCompleted, progress.CompletedAt));
    }
}
