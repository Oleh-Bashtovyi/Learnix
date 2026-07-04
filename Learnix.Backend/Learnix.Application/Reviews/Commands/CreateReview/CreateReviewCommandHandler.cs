using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.Reviews.Constants;
using Learnix.Application.Reviews.Specifications;
using Learnix.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    ICourseReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : IRequestHandler<CreateReviewCommand, Result<CreateReviewResponse>>
{
    public async Task<Result<CreateReviewResponse>> Handle(
        CreateReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, forUpdate: true), cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        if (course.InstructorId == studentId)
            return Result.Fail(new ForbiddenError(ReviewMessages.InstructorsCannotReviewOwnCourses));

        var isEnrolled = await enrollmentRepository.AnyAsync(
            new EnrollmentByStudentAndCourseSpecification(studentId, request.CourseId), cancellationToken);

        if (!isEnrolled)
            return Result.Fail(new ForbiddenError(CommonMessages.NotEnrolledInCourse));

        var alreadyReviewed = await reviewRepository.AnyAsync(
            new CourseReviewByStudentAndCourseSpecification(studentId, request.CourseId), cancellationToken);

        if (alreadyReviewed)
            return Result.Fail(new ConflictError(ReviewMessages.AlreadyReviewed));

        var review = CourseReview.Create(request.CourseId, studentId, request.Rating, request.Comment);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await reviewRepository.AddAsync(review, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var metrics = await reviewRepository.GetCourseRatingMetricsAsync(request.CourseId, cancellationToken);
            course.SyncRating(metrics.Count, metrics.Average);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await cache.RemoveAsync(CacheKeys.Course(request.CourseId), cancellationToken);

        return Result.Ok(new CreateReviewResponse(review.Id));
    }
}
