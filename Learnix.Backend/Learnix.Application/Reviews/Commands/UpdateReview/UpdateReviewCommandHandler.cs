using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Reviews.Abstractions;
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
            return Result.Fail(new NotFoundError("Review not found."));

        if (review.StudentId != currentUser.UserId.Value)
            return Result.Fail(new ForbiddenError("You can only edit your own reviews."));

        var oldRating = review.Rating;
        review.Update(request.Rating, request.Comment);

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, forUpdate: true), cancellationToken);

        if (course is not null)
            course.UpdateRating(oldRating, request.Rating);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(CacheKeys.Course(request.CourseId), cancellationToken);

        return Result.Ok();
    }
}
