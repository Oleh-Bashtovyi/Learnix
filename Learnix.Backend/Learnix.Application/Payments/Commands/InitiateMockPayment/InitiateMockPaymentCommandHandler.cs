using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Constants;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.Payments.Commands.InitiateMockPayment;

public sealed class InitiateMockPaymentCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IPaymentRepository paymentRepository,
    IWishlistRepository wishlistRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<InitiateMockPaymentCommand, Result<InitiateMockPaymentResponse>>
{
    public async Task<Result<InitiateMockPaymentResponse>> Handle(
        InitiateMockPaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, forUpdate: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CourseMessages.CourseIdNotFound(request.CourseId)));

        if (course.Status != CourseStatus.Published)
            return Result.Fail(new ConflictError(PaymentMessages.OnlyPublishedCanBePurchased));

        if (course.Price <= 0m)
            return Result.Fail(new ConflictError(PaymentMessages.CourseIsFreeUseEnrollment));

        var alreadyEnrolled = await enrollmentRepository.AnyAsync(
            new EnrollmentByStudentAndCourseSpecification(userId, request.CourseId),
            cancellationToken);

        if (alreadyEnrolled)
            return Result.Fail(new ConflictError(PaymentMessages.AlreadyEnrolled));

        var enrollment = Enrollment.Create(request.CourseId, userId, course.Price);
        enrollment.ConfirmPayment();

        await enrollmentRepository.AddAsync(enrollment, cancellationToken);

        var payment = Payment.CreateMock(userId, request.CourseId, enrollment.Id, course.Price);
        await paymentRepository.AddAsync(payment, cancellationToken);

        course.IncrementEnrollmentsCount();
        await wishlistRepository.RemoveIfExistsAsync(userId, request.CourseId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new InitiateMockPaymentResponse(payment.Id, enrollment.Id));
    }
}
