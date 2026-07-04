using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Constants;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Application.Wishlist.Constants;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.Wishlist.Commands.AddToWishlist;

public sealed class AddToWishlistCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IWishlistRepository wishlistRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddToWishlistCommand, Result>
{
    public async Task<Result> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CourseMessages.CourseIdNotFound(request.CourseId)));

        if (course.Status != CourseStatus.Published)
            return Result.Fail(new ConflictError(WishlistMessages.OnlyPublishedAddedToWishlist));

        var alreadyEnrolled = await enrollmentRepository.AnyAsync(
            new EnrollmentByStudentAndCourseSpecification(userId, request.CourseId),
            cancellationToken);

        if (alreadyEnrolled)
            return Result.Fail(new ConflictError(EnrollmentMessages.AlreadyEnrolled));

        await wishlistRepository.AddIfMissingAsync(userId, request.CourseId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
