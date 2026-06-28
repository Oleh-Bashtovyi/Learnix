using FluentValidation;

namespace Learnix.Application.Reviews.Queries.GetCourseReviews;

public sealed class GetCourseReviewsValidator : AbstractValidator<GetCourseReviewsQuery>
{
    public GetCourseReviewsValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();

        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Take)
            .GreaterThan(0)
            .LessThanOrEqualTo(Learnix.Application.Common.Constants.PaginationConstants.MaxPageSize);
    }
}
