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
using Learnix.Domain.Enums;
using MediatR;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.LessonProgress.Commands.MarkLessonComplete;

public sealed class MarkLessonCompleteCommandHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    ILessonProgressRepository lessonProgressRepository,
    ICertificateRepository certificateRepository,
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

        var wasAlreadyCompleted = progress?.IsCompleted ?? false;

        if (progress is null)
        {
            progress = LessonProgressEntity.Create(request.CourseId, request.LessonId, studentId);
            progress.MarkCompleted();
            await lessonProgressRepository.AddAsync(progress, cancellationToken);
        }
        else
        {
            progress.MarkCompleted();
        }

        if (!wasAlreadyCompleted)
            await TryIssueCertificateAsync(studentId, request.CourseId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new MarkLessonCompleteResponse(progress.Id, progress.IsCompleted, progress.CompletedAt));
    }

    private async Task TryIssueCertificateAsync(Guid studentId, Guid courseId, CancellationToken ct)
    {
        var visibleCount = await lessonRepository.GetVisibleLessonCountAsync(courseId, ct);
        if (visibleCount == 0) return;

        // Count already-completed lessons in DB; the current lesson will make it +1 after save
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
