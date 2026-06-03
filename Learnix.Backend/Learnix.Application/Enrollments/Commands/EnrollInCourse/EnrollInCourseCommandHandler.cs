using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.Enrollments.Commands.EnrollInCourse;

public sealed class EnrollInCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IWishlistRepository wishlistRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<EnrollInCourseCommand, Result<EnrollInCourseResponse>>
{
    public async Task<Result<EnrollInCourseResponse>> Handle(
        EnrollInCourseCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, forUpdate: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        if (course.Status != CourseStatus.Published)
            return Result.Fail(new ConflictError(CommonMessages.CourseNotPublished));

        if (course.InstructorId == studentId)
            return Result.Fail(new ForbiddenError("Instructors cannot enroll in their own courses."));

        var alreadyEnrolled = await enrollmentRepository.AnyAsync(
            new EnrollmentByStudentAndCourseSpecification(studentId, request.CourseId),
            cancellationToken);

        if (alreadyEnrolled)
            return Result.Fail(new ConflictError(CommonMessages.AlreadyEnrolled));

        var enrollment = Enrollment.Create(request.CourseId, studentId, course.Price);

        // Mock payment: paid courses are auto-confirmed immediately (no Stripe yet).
        if (course.Price > 0m)
            enrollment.ConfirmPayment();

        course.IncrementEnrollmentsCount();

        await enrollmentRepository.AddAsync(enrollment, cancellationToken);
        await wishlistRepository.RemoveIfExistsAsync(studentId, request.CourseId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new EnrollInCourseResponse(enrollment.Id));
    }
}
