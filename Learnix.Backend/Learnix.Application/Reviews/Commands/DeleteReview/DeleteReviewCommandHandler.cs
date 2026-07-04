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
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ICourseReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : IRequestHandler<DeleteReviewCommand, Result>
{
    public async Task<Result> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var review = await reviewRepository.FirstOrDefaultAsync(
            new CourseReviewByIdSpecification(request.ReviewId, forUpdate: true), cancellationToken);

        if (review is null || review.CourseId != request.CourseId)
            return Result.Fail(new NotFoundError(ReviewMessages.ReviewNotFound));

        var isAdmin = currentUser.IsInRole(Roles.Admin);
        if (review.StudentId != currentUser.UserId.Value && !isAdmin)
            return Result.Fail(new ForbiddenError(ReviewMessages.CanOnlyDeleteOwnReviews));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, forUpdate: true), cancellationToken);

        if (course is not null)
        {
            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await reviewRepository.DeleteAsync(review, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                var metrics = await reviewRepository.GetCourseRatingMetricsAsync(request.CourseId, cancellationToken);
                course.SyncRating(metrics.Count, metrics.Average);

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }, cancellationToken);
        }

        await cache.RemoveAsync(CacheKeys.Course(request.CourseId), cancellationToken);

        return Result.Ok();
    }
}
