using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.Reviews.Specifications;
using MediatR;

namespace Learnix.Application.Reviews.Queries.GetMyReview;

public sealed class GetMyReviewQueryHandler(
    ICurrentUserService currentUser,
    ICourseReviewRepository reviewRepository)
    : IRequestHandler<GetMyReviewQuery, Result<MyReviewDto?>>
{
    public async Task<Result<MyReviewDto?>> Handle(GetMyReviewQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var review = await reviewRepository.FirstOrDefaultAsync(
            new CourseReviewByStudentAndCourseSpecification(currentUser.UserId.Value, request.CourseId),
            cancellationToken);

        if (review is null)
            return Result.Ok<MyReviewDto?>(null);

        return Result.Ok<MyReviewDto?>(new MyReviewDto(
            review.Id,
            review.Rating,
            review.Comment,
            review.CreatedAt,
            review.UpdatedAt));
    }
}
