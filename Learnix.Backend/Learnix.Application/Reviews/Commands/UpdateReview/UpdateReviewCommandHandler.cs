using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.Reviews.Constants;
using Learnix.Application.Reviews.Specifications;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ICourseReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : IRequestHandler<UpdateReviewCommand, Result>
{
    public async Task<Result> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var review = await reviewRepository.FirstOrDefaultAsync(
            new CourseReviewByIdSpecification(request.ReviewId, forUpdate: true), cancellationToken);

        if (review is null || review.CourseId != request.CourseId)
            return Result.Fail(new NotFoundError(ReviewMessages.ReviewNotFound));

        if (review.StudentId != currentUser.UserId.Value)
            return Result.Fail(new ForbiddenError(ReviewMessages.CanOnlyEditOwnReviews));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, forUpdate: true), cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            review.Update(request.Rating, request.Comment);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // The course can be soft-deleted out from under a review that still exists. The edit is still
            // the student's to make; there is simply no denormalized rating left to keep in step.
            if (course is null)
                return;

            var metrics = await reviewRepository.GetCourseRatingMetricsAsync(request.CourseId, cancellationToken);
            course.SyncRating(metrics.Count, metrics.Average);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await cache.RemoveAsync(CacheKeys.Courses.ById(request.CourseId), cancellationToken);

        return Result.Ok();
    }
}
